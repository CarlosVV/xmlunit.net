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
using System.Xml;
using Org.XmlUnit.Util;
using NUnit.Framework;

namespace Org.XmlUnit.Diff {

    [TestFixture]
    public class XPathContextTest {
        [Test]
        public void Empty() {
            Assert.AreEqual("/", new XPathContext().XPath);
        }

        [Test]
        public void RootElement() {
            XmlDocument doc = new XmlDocument();
            Assert.AreEqual("/foo[1]",
                            new XPathContext(doc.CreateElement("foo"))
                            .XPath);
        }

        [Test]
        public void AppendChildren() {
            List<Element> l = new List<Element>();
            l.Add(new Element("foo"));
            l.Add(new Element("foo"));
            XPathContext ctx = new XPathContext();
            ctx.SetChildren(l);
            l = new List<Element>();
            l.Add(new Element("bar"));
            l.Add(new Element("foo"));
            ctx.AppendChildren(l);
            ctx.NavigateToChild(0);
            Assert.AreEqual("/foo[1]", ctx.XPath);
            ctx.NavigateToParent();
            ctx.NavigateToChild(1);
            Assert.AreEqual("/foo[2]", ctx.XPath);
            ctx.NavigateToParent();
            ctx.NavigateToChild(2);
            Assert.AreEqual("/bar[1]", ctx.XPath);
            ctx.NavigateToParent();
            ctx.NavigateToChild(3);
            Assert.AreEqual("/foo[3]", ctx.XPath);
        }

        [Test]
        public void AppendChildrenWithNonElements() {
            List<XPathContext.INodeInfo> l = new List<XPathContext.INodeInfo>();
            l.Add(new Text());
            l.Add(new Comment());
            l.Add(new CDATA());
            l.Add(new PI());
            XPathContext ctx = new XPathContext();
            ctx.SetChildren(l);
            ctx.AppendChildren(l);
            ctx.NavigateToChild(0);
            Assert.AreEqual("/text()[1]", ctx.XPath);
            ctx.NavigateToParent();
            ctx.NavigateToChild(1);
            Assert.AreEqual("/comment()[1]", ctx.XPath);
            ctx.NavigateToParent();
            ctx.NavigateToChild(2);
            Assert.AreEqual("/text()[2]", ctx.XPath);
            ctx.NavigateToParent();
            ctx.NavigateToChild(3);
            Assert.AreEqual("/processing-instruction()[1]", ctx.XPath);
            ctx.NavigateToParent();
            ctx.NavigateToChild(4);
            Assert.AreEqual("/text()[3]", ctx.XPath);
            ctx.NavigateToParent();
            ctx.NavigateToChild(5);
            Assert.AreEqual("/comment()[2]", ctx.XPath);
            ctx.NavigateToParent();
            ctx.NavigateToChild(6);
            Assert.AreEqual("/text()[4]", ctx.XPath);
            ctx.NavigateToParent();
            ctx.NavigateToChild(7);
            Assert.AreEqual("/processing-instruction()[2]", ctx.XPath);
        }

        [Test]
        public void OneLevelOfElements() {
            List<Element> l = new List<Element>();
            l.Add(new Element("foo"));
            l.Add(new Element("foo"));
            l.Add(new Element("bar"));
            l.Add(new Element("foo"));
            XPathContext ctx = new XPathContext();
            ctx.SetChildren(l);
            ctx.NavigateToChild(0);
            Assert.AreEqual("/foo[1]", ctx.XPath);
            ctx.NavigateToParent();
            ctx.NavigateToChild(1);
            Assert.AreEqual("/foo[2]", ctx.XPath);
            ctx.NavigateToParent();
            ctx.NavigateToChild(2);
            Assert.AreEqual("/bar[1]", ctx.XPath);
            ctx.NavigateToParent();
            ctx.NavigateToChild(3);
            Assert.AreEqual("/foo[3]", ctx.XPath);
        }

        [Test]
        public void TwoLevelsOfElements() {
            List<Element> l = new List<Element>();
            l.Add(new Element("foo"));
            l.Add(new Element("foo"));
            l.Add(new Element("bar"));
            l.Add(new Element("foo"));
            XPathContext ctx = new XPathContext();
            ctx.SetChildren(l);
            ctx.NavigateToChild(0);
            Assert.AreEqual("/foo[1]", ctx.XPath);
            ctx.SetChildren(l);
            ctx.NavigateToChild(3);
            Assert.AreEqual("/foo[1]/foo[3]", ctx.XPath);
            ctx.NavigateToParent();
            Assert.AreEqual("/foo[1]", ctx.XPath);
            ctx.NavigateToParent();
            ctx.NavigateToChild(2);
            Assert.AreEqual("/bar[1]", ctx.XPath);
        }

        [Test]
        public void Attributes() {
            XPathContext ctx = new XPathContext();
            ctx.SetChildren(Linqy.Singleton(new Element("foo")));
            ctx.NavigateToChild(0);
            List<XmlQualifiedName> l = new List<XmlQualifiedName>();
            l.Add(new XmlQualifiedName("bar"));
            ctx.AddAttributes(l);
            ctx.NavigateToAttribute(new XmlQualifiedName("bar"));
            Assert.AreEqual("/foo[1]/@bar", ctx.XPath);
        }

        [Test]
        public void singleAttribute() {
            XPathContext ctx = new XPathContext();
            ctx.SetChildren(Linqy.Singleton(new Element("foo")));
            ctx.NavigateToChild(0);
            ctx.AddAttribute(new XmlQualifiedName("bar"));
            ctx.NavigateToAttribute(new XmlQualifiedName("bar"));
            Assert.AreEqual("/foo[1]/@bar", ctx.XPath);
        }

        [Test]
        public void Mixed() {
            List<XPathContext.INodeInfo> l = new List<XPathContext.INodeInfo>();
            l.Add(new Text());
            l.Add(new Comment());
            l.Add(new CDATA());
            l.Add(new PI());
            l.Add(new CDATA());
            l.Add(new Comment());
            l.Add(new PI());
            l.Add(new Text());
            XPathContext ctx = new XPathContext();
            ctx.SetChildren(l);
            ctx.NavigateToChild(0);
            Assert.AreEqual("/text()[1]", ctx.XPath);
            ctx.NavigateToParent();
            ctx.NavigateToChild(1);
            Assert.AreEqual("/comment()[1]", ctx.XPath);
            ctx.NavigateToParent();
            ctx.NavigateToChild(2);
            Assert.AreEqual("/text()[2]", ctx.XPath);
            ctx.NavigateToParent();
            ctx.NavigateToChild(3);
            Assert.AreEqual("/processing-instruction()[1]", ctx.XPath);
            ctx.NavigateToParent();
            ctx.NavigateToChild(4);
            Assert.AreEqual("/text()[3]", ctx.XPath);
            ctx.NavigateToParent();
            ctx.NavigateToChild(5);
            Assert.AreEqual("/comment()[2]", ctx.XPath);
            ctx.NavigateToParent();
            ctx.NavigateToChild(6);
            Assert.AreEqual("/processing-instruction()[2]", ctx.XPath);
            ctx.NavigateToParent();
            ctx.NavigateToChild(7);
            Assert.AreEqual("/text()[4]", ctx.XPath);
        }

        [Test]
        public void ElementsAndNs() {
            List<Element> l = new List<Element>();
            l.Add(new Element("foo", "urn:foo:foo"));
            l.Add(new Element("foo"));
            l.Add(new Element("foo", "urn:foo:bar"));
            Dictionary<string, string> m = new Dictionary<string, string>();
            m["bar"] = "urn:foo:bar";
            XPathContext ctx = new XPathContext(m);
            ctx.SetChildren(l);
            ctx.NavigateToChild(0);
            Assert.AreEqual("/foo[1]", ctx.XPath);
            ctx.NavigateToParent();
            ctx.NavigateToChild(1);
            Assert.AreEqual("/foo[2]", ctx.XPath);
            ctx.NavigateToParent();
            ctx.NavigateToChild(2);
            Assert.AreEqual("/bar:foo[1]", ctx.XPath);
        }

        [Test]
        public void AttributesAndNs() {
            Dictionary<string, string> m = new Dictionary<string, string>();
            m["bar"] = "urn:foo:bar";
            XPathContext ctx = new XPathContext(m);
            ctx.SetChildren(Linqy.Singleton(new Element("foo",
                                                             "urn:foo:bar")));
            ctx.NavigateToChild(0);
            List<XmlQualifiedName> l = new List<XmlQualifiedName>();
            l.Add(new XmlQualifiedName("baz"));
            l.Add(new XmlQualifiedName("baz", "urn:foo:bar"));
            ctx.AddAttributes(l);
            ctx.NavigateToAttribute(new XmlQualifiedName("baz"));
            Assert.AreEqual("/bar:foo[1]/@baz", ctx.XPath);
            ctx.NavigateToParent();
            ctx.NavigateToAttribute(new XmlQualifiedName("baz", "urn:foo:bar"));
            Assert.AreEqual("/bar:foo[1]/@bar:baz", ctx.XPath);
            ctx.NavigateToParent();
        }

        [Test]
        public void ShouldCreateCopyOnClone() {
            List<Element> l = new List<Element>();
            l.Add(new Element("foo"));
            l.Add(new Element("foo"));
            l.Add(new Element("bar"));
            XPathContext ctx = new XPathContext();
            ctx.SetChildren(l);
            ctx.NavigateToChild(1);
            Assert.AreEqual("/foo[2]", ctx.XPath);
            XPathContext clone = (XPathContext) ctx.Clone();
            Assert.AreEqual("/foo[2]", clone.XPath);
            Assert.AreNotSame(ctx, clone);
            clone.NavigateToParent();
            clone.NavigateToChild(2);
            Assert.AreEqual("/bar[1]", clone.XPath);
            Assert.AreEqual("/foo[2]", ctx.XPath);
        }

        internal class Element : XPathContext.INodeInfo {
            private readonly XmlQualifiedName name;
            internal Element(string name) {
                this.name = new XmlQualifiedName(name);
            }
            internal Element(string name, string ns) {
                this.name = new XmlQualifiedName(name, ns);
            }
            public XmlQualifiedName Name { get { return name; } }
            public XmlNodeType Type { get { return XmlNodeType.Element; } }
        }

        internal abstract class NonElement : XPathContext.INodeInfo {
            public XmlQualifiedName Name { get { return null; } }
            public abstract XmlNodeType Type { get; }
        }
        internal class Text : NonElement {
            public override XmlNodeType Type {
                get { return XmlNodeType.Text; }
            }
        }
        internal class Comment : NonElement {
            public override XmlNodeType Type {
                get { return XmlNodeType.Comment; }
            }
        }
        internal class PI : NonElement {
            public override XmlNodeType Type {
                get { return XmlNodeType.ProcessingInstruction;}
            }
        }
        internal class CDATA : NonElement {
            public override XmlNodeType Type {
                get { return XmlNodeType.CDATA; }
            }
        }
    }
}