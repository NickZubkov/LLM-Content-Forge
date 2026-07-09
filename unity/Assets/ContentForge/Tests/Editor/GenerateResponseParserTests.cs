using ContentForge.Editor;
using NUnit.Framework;

namespace ContentForge.Editor.Tests
{
    public sealed class GenerateResponseParserTests
    {
        [Test]
        public void Parse_ItemResponse_ReturnsTypedItems()
        {
            const string json =
                "{\"contentType\":\"Item\",\"content\":[" +
                "{\"name\":\"Frost Blade\",\"description\":\"cold\",\"rarity\":\"rare\",\"power\":7,\"value\":100}]}";

            var result = GenerateResponseParser.Parse(json);

            Assert.That(result.ContentType, Is.EqualTo(GeneratedContentType.Item));
            Assert.That(result.Enemies, Is.Null);
            Assert.That(result.Items, Has.Count.EqualTo(1));
            Assert.That(result.Items[0].Name, Is.EqualTo("Frost Blade"));
            Assert.That(result.Items[0].Power, Is.EqualTo(7));
        }

        [Test]
        public void Parse_EnemyResponse_ReturnsTypedEnemies()
        {
            const string json =
                "{\"contentType\":\"Enemy\",\"content\":[" +
                "{\"name\":\"Ice Wraith\",\"description\":\"boo\",\"level\":3,\"health\":40,\"damage\":8}]}";

            var result = GenerateResponseParser.Parse(json);

            Assert.That(result.ContentType, Is.EqualTo(GeneratedContentType.Enemy));
            Assert.That(result.Items, Is.Null);
            Assert.That(result.Enemies, Has.Count.EqualTo(1));
            Assert.That(result.Enemies[0].Health, Is.EqualTo(40));
        }

        [Test]
        public void Parse_EmptyBody_Throws()
        {
            Assert.Throws<GenerateResponseParseException>(() => GenerateResponseParser.Parse("   "));
        }

        [Test]
        public void Parse_NotJson_Throws()
        {
            Assert.Throws<GenerateResponseParseException>(() => GenerateResponseParser.Parse("not json"));
        }

        [Test]
        public void Parse_EmptyContentArray_Throws()
        {
            Assert.Throws<GenerateResponseParseException>(
                () => GenerateResponseParser.Parse("{\"contentType\":\"Item\",\"content\":[]}"));
        }

        [Test]
        public void Parse_UnknownContentType_Throws()
        {
            Assert.Throws<GenerateResponseParseException>(
                () => GenerateResponseParser.Parse("{\"contentType\":\"Spell\",\"content\":[{}]}"));
        }
    }
}
