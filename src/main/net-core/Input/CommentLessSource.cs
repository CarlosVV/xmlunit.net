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
using System.Xml;
using Org.XmlUnit.Transform;

namespace Org.XmlUnit.Input {

    /// <summary>
    /// ISource implementation that is obtained from a different
    /// source by stripping all comments.
    /// </summary>
    public sealed class CommentLessSource : ISource {
        private readonly XmlReader reader;
        private string systemId;

        /// <summary>
        /// Creates a new Source with the same content as another source removing all comments.
        /// </summary>
        /// <param name="originalSource">source with the original content</param>
        public CommentLessSource(ISource originalSource)
        {
            if (originalSource == null) {
                throw new ArgumentNullException();
            }
            systemId = originalSource.SystemId;

            Transformation t = new Transformation(originalSource);
            t.Stylesheet = Stylesheet;
            reader = new XmlNodeReader(t.TransformToDocument());
        }

        /// <inheritdoc/>
        public XmlReader Reader
        {
            get {
                return reader;
            }
        }

        /// <inheritdoc/>
        public string SystemId
        {
            get {
                return systemId;
            }
            set {
                systemId = value;
            }
        }

        private const string STYLE =
            "<stylesheet xmlns=\"http://www.w3.org/1999/XSL/Transform\" version=\"1.0\">"
            + "<template match=\"node()[not(self::comment())]|@*\"><copy>"
            + "<apply-templates select=\"node()[not(self::comment())]|@*\"/>"
            + "</copy></template>"
            + "</stylesheet>";

        private static ISource Stylesheet {
            get {
                return new StreamSource(new System.IO.StringReader(STYLE));
            }
        }
    }
}
