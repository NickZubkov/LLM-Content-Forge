using ContentForge.Editor;
using NUnit.Framework;

namespace ContentForge.Editor.Tests
{
    public sealed class ContentNamingTests
    {
        [Test]
        public void Slugify_LowercasesAndHyphenatesWords()
        {
            Assert.That(ContentNaming.Slugify("Frost Blade"), Is.EqualTo("frost-blade"));
        }

        [Test]
        public void Slugify_CollapsesRunsOfSymbolsAndTrims()
        {
            Assert.That(ContentNaming.Slugify("  A_B!! C "), Is.EqualTo("a-b-c"));
        }

        [Test]
        public void Slugify_ReturnsEmptyForNullOrWhitespace()
        {
            Assert.That(ContentNaming.Slugify(null), Is.EqualTo(string.Empty));
            Assert.That(ContentNaming.Slugify("   "), Is.EqualTo(string.Empty));
        }
    }
}
