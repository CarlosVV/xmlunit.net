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
using NUnit.Framework;
using Org.XmlUnit.Input;

namespace Org.XmlUnit.Constraints {
    [TestFixture]
    public class ValidationConstraintTest {
        [Test]
        public void ShouldSuccessfullyValidateInstance() {
            Assert.That(new StreamSource(TestResources.TESTS_DIR
                                         + "BookXsdGeneratedNoSchema.xml"),
                        new SchemaValidConstraint(
                            new StreamSource(TestResources.TESTS_DIR + "Book.xsd")));
        }

        [Test]
        public void ShouldFailOnBrokenSchema() {
            Assert.That(new StreamSource(TestResources.TESTS_DIR
                                         + "invalidBook.xml"),
                        !new SchemaValidConstraint(new StreamSource(TestResources
                                                                    .TESTS_DIR +
                                                                    "Book.xsd")));
        }

        [Test][Ignore("Validator doesn't seem to like https URIs")]
        public void ShouldSuccessfullyValidateInstanceWithoutExplicitSchemaSource() {
            Assert.That(new StreamSource(TestResources.TESTS_DIR
                                         + "BookXsdGenerated.xml"),
                        new SchemaValidConstraint());
        }

        [Test][ExpectedException( typeof( ArgumentException ) )]
        public void ShouldThrowWhenSchemaSourcesContainsNull() {
            new SchemaValidConstraint(new object[] { null });
        }

        [Test][ExpectedException( typeof( ArgumentNullException ) )]
        public void ShouldThrowWhenSchemaSourcesIsNull() {
            new SchemaValidConstraint(null);
        }
    }
}
