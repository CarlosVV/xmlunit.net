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
using System.Xml.Linq;

namespace Org.XmlUnit.Input {
    /// <summary>
    /// ISource implementation encapsulating a System.Xml.Linq XNode.
    /// </summary>
    public class LinqSource : AbstractSource {
        private readonly XNode node;

        /// <summary>
        /// Wraps the given XNode as ISource.
        /// </summary>
        /// <param name="node">the node to wrap</param>
        public LinqSource(XNode node)
            : base(node.CreateReader()) {
            this.node = node;
        }

        /// <summary>
        /// The node this source is wrapping
        /// </summary>
        public XNode Node {
            get {
                return node;
            }
        }
    }
}
