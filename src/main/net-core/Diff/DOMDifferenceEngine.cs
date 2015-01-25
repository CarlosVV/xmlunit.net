/*
  This file is licensed to You under the Apache License, Version 2.0
  (the "License"); you may not use this file except in compliance with
  the License.  You may obtain a copy of the License at

  http://www.apache.org/licenses/LICENSE-2.0

  Unless required by applicable law or agreed to in writing, software
  distributed under the License is distributed on an "AS IS" BASIS,
  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
  See the License for the specific language governing permissions and
  limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Schema;
using Org.XmlUnit.Util;

namespace Org.XmlUnit.Diff{

    /// <summary>
    /// Difference engine based on DOM.
    /// </summary>
    public sealed class DOMDifferenceEngine : AbstractDifferenceEngine {

        public override void Compare(ISource control, ISource test) {
            if (control == null) {
                throw new ArgumentNullException("control");
            }
            if (test == null) {
                throw new ArgumentNullException("test");
            }
            try {
                XmlNode controlNode = Org.XmlUnit.Util.Convert.ToNode(control);
                XmlNode testNode = Org.XmlUnit.Util.Convert.ToNode(test);
                CompareNodes(controlNode, xpathContextFor(controlNode),
                             testNode, xpathContextFor(testNode));
            } catch (Exception ex) {
                throw new XMLUnitException("Caught exception during comparison",
                                           ex);
            }
        }

        private XPathContext xpathContextFor(XmlNode n) {
            return new XPathContext(NamespaceContext, n);
        }

        /// <summary>
        /// Recursively compares two XML nodes.
        /// </summary>
        /// <remarks>
        /// Performs comparisons common to all node types, then performs
        /// the node type specific comparisons and finally recurses into
        /// the node's child lists.
        ///
        /// Stops as soon as any comparison returns ComparisonResult.CRITICAL.
        /// </remarks>
        internal KeyValuePair<ComparisonResult, bool>
            CompareNodes(XmlNode control,
                         XPathContext controlContext,
                         XmlNode test,
                         XPathContext testContext) {
            IEnumerable<XmlNode> controlChildren =
                control.ChildNodes.Cast<XmlNode>().Where(INTERESTING_NODES);
            IEnumerable<XmlNode> testChildren =
                test.ChildNodes.Cast<XmlNode>().Where(INTERESTING_NODES);

            return new ComparisonChain(Compare(new Comparison(ComparisonType.NODE_TYPE,
                                                              control,
                                                              GetXPath(controlContext),
                                                              control.NodeType,
                                                              test, GetXPath(testContext),
                                                              test.NodeType)))
                .AndThen(Comparer(new Comparison(ComparisonType.NAMESPACE_URI,
                                                 control, GetXPath(controlContext),
                                                 control.NamespaceURI,
                                                 test, GetXPath(testContext),
                                                 test.NamespaceURI)))
                .AndThen(Comparer(new Comparison(ComparisonType.NAMESPACE_PREFIX,
                                                 control, GetXPath(controlContext),
                                                 control.Prefix,
                                                 test, GetXPath(testContext),
                                                 test.Prefix)))
                .AndIfTrueThen(control.NodeType != XmlNodeType.Attribute,
                               Comparer(new Comparison(ComparisonType.CHILD_NODELIST_LENGTH,
                                                       control, GetXPath(controlContext),
                                                       controlChildren.Count(),
                                                       test, GetXPath(testContext),
                                                       testChildren.Count())))
                .AndThen(() => NodeTypeSpecificComparison(control, controlContext,
                                                          test, testContext))
                // and finally recurse into children
                .AndIfTrueThen(control.NodeType != XmlNodeType.Attribute,
                               CompareChildren(control, controlContext,
                                               controlChildren,
                                               test, testContext,
                                               testChildren))
                .FinalResult;
        }

        /// <summary>
        /// Dispatches to the node type specific comparison if one is
        /// defined for the given combination of nodes.
        /// </summary>
        private KeyValuePair<ComparisonResult, bool>
            NodeTypeSpecificComparison(XmlNode control,
                                       XPathContext controlContext,
                                       XmlNode test, XPathContext testContext) {
            switch (control.NodeType) {
            case XmlNodeType.CDATA:
            case XmlNodeType.Comment:
            case XmlNodeType.Text:
                if (test is XmlCharacterData) {
                    return CompareCharacterData((XmlCharacterData) control,
                                                controlContext,
                                                (XmlCharacterData) test,
                                                testContext);
                }
                break;
            case XmlNodeType.Document:
                if (test is XmlDocument) {
                    return CompareDocuments((XmlDocument) control,
                                            controlContext,
                                            (XmlDocument) test, testContext);
                }
                break;
            case XmlNodeType.Element:
                if (test is XmlElement) {
                    return CompareElements((XmlElement) control,
                                           controlContext,
                                           (XmlElement) test,
                                           testContext);
                }
                break;
            case XmlNodeType.ProcessingInstruction:
                if (test is XmlProcessingInstruction) {
                    return
                        CompareProcessingInstructions((XmlProcessingInstruction) control,
                                                      controlContext,
                                                      (XmlProcessingInstruction) test,
                                                      testContext);
                }
                break;
            case XmlNodeType.DocumentType:
                if (test is XmlDocumentType) {
                    return CompareDocTypes((XmlDocumentType) control,
                                           controlContext,
                                           (XmlDocumentType) test, testContext);
                }
                break;
            case XmlNodeType.Attribute:
                if (test is XmlAttribute) {
                    return CompareAttributes((XmlAttribute) control,
                                             controlContext,
                                             (XmlAttribute) test, testContext);
                }
                break;
            }
            return new KeyValuePair<ComparisonResult, bool>(ComparisonResult.EQUAL, false);
        }

        private Func<KeyValuePair<ComparisonResult, bool>>
            CompareChildren(XmlNode control,
                            XPathContext controlContext,
                            IEnumerable<XmlNode> controlChildren,
                            XmlNode test,
                            XPathContext testContext,
                            IEnumerable<XmlNode> testChildren) {

            return () => {
                controlContext
                    .SetChildren(controlChildren.Select(TO_NODE_INFO));
                testContext
                    .SetChildren(testChildren.Select(TO_NODE_INFO));
                return CompareNodeLists(controlChildren, controlContext,
                                        testChildren, testContext);
            };
        }

        /// <summary>
        /// Compares textual content.
        /// </summary>
        private KeyValuePair<ComparisonResult, bool>
            CompareCharacterData(XmlCharacterData control,
                                 XPathContext controlContext,
                                 XmlCharacterData test,
                                 XPathContext testContext) {
            return Compare(new Comparison(ComparisonType.TEXT_VALUE, control,
                                          GetXPath(controlContext),
                                          control.Data,
                                          test, GetXPath(testContext),
                                          test.Data));
        }

        /// <summary>
        /// Compares document node, doctype and XML declaration properties
        /// </summary>
        private KeyValuePair<ComparisonResult, bool>
            CompareDocuments(XmlDocument control,
                             XPathContext controlContext,
                             XmlDocument test,
                             XPathContext testContext) {
            XmlDocumentType controlDt = control.DocumentType;
            XmlDocumentType testDt = test.DocumentType;

            return new ComparisonChain(
                Compare(new Comparison(ComparisonType.HAS_DOCTYPE_DECLARATION,
                                       control, GetXPath(controlContext),
                                       controlDt != null,
                                       test, GetXPath(testContext),
                                       testDt != null)))
                .AndIfTrueThen(controlDt != null && testDt != null,
                               () => CompareNodes(controlDt, controlContext,
                                                  testDt, testContext))
                .AndThen(() =>
                         CompareDeclarations(control.FirstChild as XmlDeclaration,
                                             controlContext,
                                             test.FirstChild as XmlDeclaration,
                                             testContext))
                .FinalResult;
        }

        /// <summary>
        /// Compares properties of the doctype declaration.
        /// </summary>
        private KeyValuePair<ComparisonResult, bool>
            CompareDocTypes(XmlDocumentType control,
                            XPathContext controlContext,
                            XmlDocumentType test,
                            XPathContext testContext) {
            return new ComparisonChain(
                Compare(new Comparison(ComparisonType.DOCTYPE_NAME,
                                       control, GetXPath(controlContext),
                                       control.Name,
                                       test, GetXPath(testContext),
                                       test.Name)))
                .AndThen(Comparer(new Comparison(ComparisonType.DOCTYPE_PUBLIC_ID,
                                                 control, GetXPath(controlContext),
                                                 control.PublicId,
                                                 test, GetXPath(testContext),
                                                 test.PublicId)))
                .AndThen(Comparer(new Comparison(ComparisonType.DOCTYPE_SYSTEM_ID,
                                                 control, GetXPath(controlContext),
                                                 control.SystemId,
                                                 test, GetXPath(testContext),
                                                 test.SystemId)))
                .FinalResult;
        }

        /// <summary>
        /// Compares properties of XML declaration.
        /// </summary>
        private KeyValuePair<ComparisonResult, bool>
            CompareDeclarations(XmlDeclaration control,
                                XPathContext controlContext,
                                XmlDeclaration test,
                                XPathContext testContext) {
            string controlVersion =
                control == null ? "1.0" : control.Version;
            string testVersion =
                test == null ? "1.0" : test.Version;
            string controlStandalone =
                control == null ? string.Empty : control.Standalone;
            string testStandalone =
                test == null ? string.Empty : test.Standalone;
            string controlEncoding =
                control != null ? control.Encoding : string.Empty;
            string testEncoding = test != null ? test.Encoding : string.Empty;

            return new ComparisonChain(
                Compare(new Comparison(ComparisonType.XML_VERSION,
                                       control, GetXPath(controlContext),
                                       controlVersion,
                                       test, GetXPath(testContext),
                                       testVersion)))
                .AndThen(Comparer(new Comparison(ComparisonType.XML_STANDALONE,
                                                 control, GetXPath(controlContext),
                                                 controlStandalone,
                                                 test, GetXPath(testContext),
                                                 testStandalone)))
                .AndThen(Comparer(new Comparison(ComparisonType.XML_ENCODING,
                                                 control, GetXPath(controlContext),
                                                 controlEncoding,
                                                 test, GetXPath(testContext),
                                                 testEncoding)))
                .FinalResult;
        }

        /// <summary>
        /// Compares element's node properties, in particular the
        /// element's name and its attributes.
        /// </summary>
        private KeyValuePair<ComparisonResult, bool>
            CompareElements(XmlElement control,
                            XPathContext controlContext,
                            XmlElement test,
                            XPathContext testContext) {
            return new ComparisonChain(
                Compare(new Comparison(ComparisonType.ELEMENT_TAG_NAME,
                                       control, GetXPath(controlContext),
                                       control.Name,
                                       test, GetXPath(testContext),
                                       test.Name)))
                .AndThen(() => CompareElementAttributes(control, controlContext,
                                                        test, testContext))
                .FinalResult;
        }

        /// <summary>
        /// Compares element's attributes.
        /// </summary>
        private KeyValuePair<ComparisonResult, bool>
            CompareElementAttributes(XmlElement control,
                                     XPathContext controlContext,
                                     XmlElement test,
                                     XPathContext testContext) {
            Attributes controlAttributes = SplitAttributes(control.Attributes);
            controlContext
                .AddAttributes(controlAttributes.RemainingAttributes
                               .Select(Nodes.GetQName));
            Attributes testAttributes = SplitAttributes(test.Attributes);
            testContext
                .AddAttributes(testAttributes.RemainingAttributes
                               .Select(Nodes.GetQName));

            return new ComparisonChain(
                Compare(new Comparison(ComparisonType.ELEMENT_NUM_ATTRIBUTES,
                                       control, GetXPath(controlContext),
                                       controlAttributes.RemainingAttributes.Count,
                                       test, GetXPath(testContext),
                                       testAttributes.RemainingAttributes.Count)))
                .AndThen(() => CompareXsiType(controlAttributes.Type, controlContext,
                                              testAttributes.Type, testContext))
                .AndThen(Comparer(new Comparison(ComparisonType.SCHEMA_LOCATION,
                                                 control, GetXPath(controlContext),
                                                 controlAttributes.SchemaLocation != null 
                                                 ? controlAttributes.SchemaLocation.Value 
                                                 : null,
                                                 test, GetXPath(testContext),
                                                 testAttributes.SchemaLocation != null
                                                 ? testAttributes.SchemaLocation.Value
                                                 : null)))
                .AndThen(Comparer(new Comparison(ComparisonType.NO_NAMESPACE_SCHEMA_LOCATION,
                                                 control, GetXPath(controlContext),
                                                 controlAttributes.NoNamespaceSchemaLocation != null
                                                 ? controlAttributes.NoNamespaceSchemaLocation.Value
                                                 : null,
                                                 test, GetXPath(testContext),
                                                 testAttributes.NoNamespaceSchemaLocation != null
                                                 ? testAttributes.NoNamespaceSchemaLocation.Value
                                                 : null)))
                .AndThen(NormalAttributeComparer(control, controlContext,
                                                 controlAttributes,
                                                 test, testContext,
                                                 testAttributes))
                .FinalResult;
        }

        private Func<KeyValuePair<ComparisonResult, bool>>
            NormalAttributeComparer(XmlElement control,
                                    XPathContext controlContext,
                                    Attributes controlAttributes,
                                    XmlElement test,
                                    XPathContext testContext,
                                    Attributes testAttributes) {
            return () => {
                ComparisonChain chain = new ComparisonChain();
                ISet<XmlAttribute> foundTestAttributes = new HashSet<XmlAttribute>();

                foreach (XmlAttribute controlAttr
                         in controlAttributes.RemainingAttributes) {
                    XmlQualifiedName controlAttrName = Nodes.GetQName(controlAttr);
                    XmlAttribute testAttr =
                        FindMatchingAttr(testAttributes.RemainingAttributes,
                                         controlAttr);
                    XmlQualifiedName testAttrName = testAttr != null
                        ? Nodes.GetQName(testAttr) : null;

                    controlContext.NavigateToAttribute(controlAttrName);
                    try {
                        chain.AndThen(Comparer(new Comparison(ComparisonType.ATTR_NAME_LOOKUP,
                                                              control, GetXPath(controlContext),
                                                              controlAttrName,
                                                              test, GetXPath(testContext),
                                                              testAttrName)));

                        if (testAttr != null) {
                            testContext.NavigateToAttribute(testAttrName);
                            try {
                                chain.AndThen(() =>
                                              CompareNodes(controlAttr, controlContext,
                                                           testAttr, testContext));

                                foundTestAttributes.Add(testAttr);
                            } finally {
                                testContext.NavigateToParent();
                            }
                        }
                    } finally {
                        controlContext.NavigateToParent();
                    }
                }
                return chain.AndThen(() => {
                        ComparisonChain secondChain = new ComparisonChain();
                        foreach (XmlAttribute testAttr
                                 in testAttributes.RemainingAttributes) {
                            if (!foundTestAttributes.Contains(testAttr)) {
                                XmlQualifiedName testAttrName = Nodes.GetQName(testAttr);
                                testContext.NavigateToAttribute(testAttrName);
                                try {
                                    secondChain
                                        .AndThen(Comparer(new Comparison(ComparisonType.ATTR_NAME_LOOKUP,
                                                                         control,
                                                                         GetXPath(controlContext),
                                                                         null,
                                                                         test,
                                                                         GetXPath(testContext),
                                                                         testAttrName)));
                                } finally {
                                    testContext.NavigateToParent();
                                }
                            }
                        }
                        return secondChain.FinalResult;
                    })
                .FinalResult;
            };
        }

        /// <summary>
        /// Compares properties of a processing instruction.
        /// </summary>
        private KeyValuePair<ComparisonResult, bool>
            CompareProcessingInstructions(XmlProcessingInstruction control,
                                          XPathContext controlContext,
                                          XmlProcessingInstruction test,
                                          XPathContext testContext) {
            return new ComparisonChain(
                Compare(new Comparison(ComparisonType.PROCESSING_INSTRUCTION_TARGET,
                                       control, GetXPath(controlContext),
                                       control.Target,
                                       test, GetXPath(testContext),
                                       test.Target)))
                .AndThen(Comparer(new Comparison(ComparisonType.PROCESSING_INSTRUCTION_DATA,
                                                 control, GetXPath(controlContext),
                                                 control.Data,
                                                 test, GetXPath(testContext),
                                                 test.Data)))
                .FinalResult;
        }

        /// <summary>
        /// Matches nodes of two node lists and invokes compareNode on
        /// each pair.
        /// </summary>
        /// <remarks>
        /// Also performs CHILD_LOOKUP comparisons for each node that
        /// couldn't be matched to one of the "other" list.
        /// </remarks>
        private KeyValuePair<ComparisonResult, bool>
            CompareNodeLists(IEnumerable<XmlNode> controlSeq,
                             XPathContext controlContext,
                             IEnumerable<XmlNode> testSeq,
                             XPathContext testContext) {

            ComparisonChain chain = new ComparisonChain();

            IEnumerable<KeyValuePair<XmlNode, XmlNode>> matches =
                NodeMatcher.Match(controlSeq, testSeq);
            IList<XmlNode> controlList = new List<XmlNode>(controlSeq);
            IList<XmlNode> testList = new List<XmlNode>(testSeq);
            ISet<XmlNode> seen = new HashSet<XmlNode>();
            foreach (KeyValuePair<XmlNode, XmlNode> pair in matches) {
                XmlNode control = pair.Key;
                seen.Add(control);
                XmlNode test = pair.Value;
                seen.Add(test);
                int controlIndex = controlList.IndexOf(control);
                int testIndex = testList.IndexOf(test);
                controlContext.NavigateToChild(controlIndex);
                testContext.NavigateToChild(testIndex);
                try {
                    chain.AndThen(Comparer(new Comparison(ComparisonType.CHILD_NODELIST_SEQUENCE,
                                                          control, GetXPath(controlContext),
                                                          controlIndex,
                                                          test, GetXPath(testContext),
                                                          testIndex)))
                        .AndThen(() => CompareNodes(control, controlContext,
                                                    test, testContext));
                } finally {
                    testContext.NavigateToParent();
                    controlContext.NavigateToParent();
                }
            }

            return chain
                .AndThen(UnmatchedControlNodes(controlList, controlContext, seen))
                .AndThen(UnmatchedTestNodes(testList, testContext, seen))
                .FinalResult;
        }

        private Func<KeyValuePair<ComparisonResult, bool>>
            UnmatchedControlNodes(IList<XmlNode> controlList,
                                  XPathContext controlContext,
                                  ISet<XmlNode> seen) {
            return () => {
                ComparisonChain chain = new ComparisonChain();
                int controlSize = controlList.Count;
                for (int i = 0; i < controlSize; i++) {
                    if (!seen.Contains(controlList[i])) {
                        controlContext.NavigateToChild(i);
                        try {
                            chain
                                .AndThen(Comparer(new Comparison(ComparisonType.CHILD_LOOKUP,
                                                                 controlList[i],
                                                                 GetXPath(controlContext),
                                                                 Nodes
                                                                 .GetQName(controlList[i]),
                                                                 null, null, null)));
                        } finally {
                            controlContext.NavigateToParent();
                        }
                    }
                }
                return chain.FinalResult;
            };
        }

        private Func<KeyValuePair<ComparisonResult, bool>>
            UnmatchedTestNodes(IList<XmlNode> testList,
                               XPathContext testContext,
                               ISet<XmlNode> seen) {
            return () => {
                ComparisonChain chain = new ComparisonChain();
                int testSize = testList.Count;
                for (int i = 0; i < testSize; i++) {
                    if (!seen.Contains(testList[i])) {
                        testContext.NavigateToChild(i);
                        try {
                            chain
                                .AndThen(Comparer(new Comparison(ComparisonType.CHILD_LOOKUP,
                                                                 null, null, null,
                                                                 testList[i],
                                                                 GetXPath(testContext),
                                                                 Nodes
                                                                 .GetQName(testList[i]))));
                        } finally {
                            testContext.NavigateToParent();
                        }
                    }
                }
                return chain.FinalResult;
            };
        }

        /// <summary>
        /// Compares xsi:type attribute values
        /// </summary>
        private KeyValuePair<ComparisonResult, bool>
            CompareXsiType(XmlAttribute control,
                           XPathContext controlContext,
                           XmlAttribute test,
                           XPathContext testContext) {
            bool mustChangeControlContext = control != null;
            bool mustChangeTestContext = test != null;
            if (!mustChangeControlContext && !mustChangeTestContext) {
                return new KeyValuePair<ComparisonResult, bool>(ComparisonResult.EQUAL, false);
            }
            bool attributePresentOnBothSides = mustChangeControlContext
                && mustChangeTestContext;
            
            try {
                XmlQualifiedName controlAttrName = null;
                if (mustChangeControlContext) {
                    controlAttrName = Nodes.GetQName(control);
                    controlContext.AddAttribute(controlAttrName);
                    controlContext.NavigateToAttribute(controlAttrName);
                }
                XmlQualifiedName testAttrName = null;
                if (mustChangeTestContext) {
                    testAttrName = Nodes.GetQName(test);
                    testContext.AddAttribute(testAttrName);
                    testContext.NavigateToAttribute(testAttrName);
                }
                return new ComparisonChain(
                    Compare(new Comparison(ComparisonType.ATTR_NAME_LOOKUP,
                                           control, GetXPath(controlContext),
                                           controlAttrName,
                                           test, GetXPath(testContext),
                                           testAttrName)))
                    .AndIfTrueThen(attributePresentOnBothSides,
                                   CompareAttributeExplicitness(control, controlContext,
                                                                test, testContext))
                    .AndIfTrueThen(attributePresentOnBothSides,
                                   Comparer(new Comparison(ComparisonType.ATTR_VALUE,
                                                           control,
                                                           GetXPath(controlContext),
                                                           ValueAsQName(control),
                                                           test,
                                                           GetXPath(testContext),
                                                           ValueAsQName(test))))
                    .FinalResult;
            } finally {
                if (mustChangeControlContext) {
                    controlContext.NavigateToParent();
                }
                if (mustChangeTestContext) {
                    testContext.NavigateToParent();
                }
            }
        }

        /// <summary>
        /// Compares properties of an attribute.
        /// </summary>
        private KeyValuePair<ComparisonResult, bool>
            CompareAttributes(XmlAttribute control,
                              XPathContext controlContext,
                              XmlAttribute test,
                              XPathContext testContext) {
            return new ComparisonChain(
                CompareAttributeExplicitness(control, controlContext,
                                             test, testContext)())
                .AndThen(Comparer(new Comparison(ComparisonType.ATTR_VALUE,
                                                 control, GetXPath(controlContext),
                                                 control.Value,
                                                 test, GetXPath(testContext),
                                                 test.Value)))
                .FinalResult;
        }

        /// <summary>
        // Compares whether two attributes are specified explicitly.
        /// </summary>
        private Func<KeyValuePair<ComparisonResult, bool>>
            CompareAttributeExplicitness(XmlAttribute control,
                                         XPathContext controlContext,
                                         XmlAttribute test,
                                         XPathContext testContext) {
            return
                Comparer(new Comparison(ComparisonType.ATTR_VALUE_EXPLICITLY_SPECIFIED,
                                        control, GetXPath(controlContext),
                                        control.Specified,
                                        test, GetXPath(testContext),
                                        test.Specified));
        }

        /// <summary>
        /// Separates XML namespace related attributes from "normal"
        /// attributes.
        /// </summary>
        private static Attributes SplitAttributes(XmlAttributeCollection map) {
            XmlAttribute sLoc = map.GetNamedItem("schemaLocation",
                                                 XmlSchema.InstanceNamespace)
                as XmlAttribute;
            XmlAttribute nNsLoc = map.GetNamedItem("noNamespaceSchemaLocation",
                                                   XmlSchema.InstanceNamespace)
                as XmlAttribute;
            XmlAttribute type = map.GetNamedItem("type",
                                                   XmlSchema.InstanceNamespace)
                as XmlAttribute;
            List<XmlAttribute> rest = new List<XmlAttribute>();
            foreach (XmlAttribute a in map) {
                if ("http://www.w3.org/2000/xmlns/" != a.NamespaceURI
                    && a != sLoc && a != nNsLoc && a != type) {
                    rest.Add(a);
                }
            }
            return new Attributes(sLoc, nNsLoc, type, rest);
        }

        private static XmlQualifiedName ValueAsQName(XmlAttribute attribute) {
            // split QName into prefix and local name
            string[] pieces = attribute.Value.Split(':');
            if (pieces.Length < 2) {
                // unprefixed name
                pieces = new string[] { string.Empty, pieces[0] };
            } else if (pieces.Length > 2) {
                // actually, this is not a valid QName - be lenient
                pieces = new string[] {
                    pieces[0],
                    attribute.Value.Substring(pieces[0].Length + 1)
                };
            }
            return new XmlQualifiedName(pieces[1] ?? string.Empty,
                                        attribute
                                        .GetNamespaceOfPrefix(pieces[0]));
        }

        internal class Attributes {
            internal readonly XmlAttribute SchemaLocation;
            internal readonly XmlAttribute NoNamespaceSchemaLocation;
            internal readonly XmlAttribute Type;
            internal readonly IList<XmlAttribute> RemainingAttributes;
            internal Attributes(XmlAttribute schemaLocation,
                                XmlAttribute noNamespaceSchemaLocation,
                                XmlAttribute type,
                                IList<XmlAttribute> remainingAttributes) {
                this.SchemaLocation = schemaLocation;
                this.NoNamespaceSchemaLocation = noNamespaceSchemaLocation;
                this.Type = type;
                this.RemainingAttributes = remainingAttributes;
            }
        }

        /// <summary>
        /// Find the attribute with the same namespace and local name
        /// as a given attribute in a list of attributes.
        /// </summary>
        private static XmlAttribute FindMatchingAttr(IList<XmlAttribute> attrs,
                                                     XmlAttribute attrToMatch) {
            bool hasNs = !string.IsNullOrEmpty(attrToMatch.NamespaceURI);
            string nsToMatch = attrToMatch.NamespaceURI;
            string nameToMatch = hasNs ? attrToMatch.LocalName
                : attrToMatch.Name;
            foreach (XmlAttribute a in attrs) {
                if (((!hasNs && string.IsNullOrEmpty(a.NamespaceURI))
                     ||
                     (hasNs && nsToMatch == a.NamespaceURI))
                    &&
                    ((hasNs && nameToMatch == a.LocalName)
                     ||
                     (!hasNs && nameToMatch == a.Name))
                    ) {
                    return a;
                }
            }
            return null;
        }

        /// <summary>
        /// Maps Nodes to their NodeInfo equivalent.
        /// </summary>
        private static XPathContext.INodeInfo TO_NODE_INFO(XmlNode n) {
            return new XPathContext.DOMNodeInfo(n);
        }

        /// <summary>
        /// Suppresses document-type and XML declaration nodes.
        /// </summary>
        private static bool INTERESTING_NODES(XmlNode n) {
            return n.NodeType != XmlNodeType.DocumentType
                && n.NodeType != XmlNodeType.XmlDeclaration;
        }

    }
}