namespace Fanex.Athena.IdentityServer.Core.UnitTests.Helpers
{
    using Core.Helpers;
    using IdentityServer.UnitTests.Common.DataAttributes;
    using Xunit;

    public class CryptoHelperTest
    {
        [Theory]
        [AutoMoqData]
        public void HashSHA1_WithContentAndSalt_ReturnsHashedString(string content, string salt)
        {
            var result = CryptoHelper.HashSHA1(content, salt);

            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.Equal(40, result.Length); // SHA1 produces 160-bit (20 bytes) hash, which is 40 hex characters
        }

        [Fact]
        public void HashSHA1_WithKnownInput_ReturnsExpectedHash()
        {
            var content = "hello";
            var salt = "world";

            var result = CryptoHelper.HashSHA1(content, salt);

            // Known SHA1 hash of "helloworld"
            Assert.Equal("6adfb183a4a2c94a2f92dab5ade762a47889a5a1".ToUpperInvariant(), result);
        }

        [Theory]
        [AutoMoqData]
        public void HashSHA1_WithSameInputs_ReturnsConsistentResults(string content, string salt)
        {
            var result1 = CryptoHelper.HashSHA1(content, salt);
            var result2 = CryptoHelper.HashSHA1(content, salt);

            Assert.Equal(result1, result2);
        }

        [Theory]
        [AutoMoqData]
        public void HashSHA1_WithDifferentInputs_ReturnsDifferentResults(string content1, string content2, string salt)
        {
            // Ensure content1 and content2 are different
            if (content1 == content2)
                content2 += "different";

            var result1 = CryptoHelper.HashSHA1(content1, salt);
            var result2 = CryptoHelper.HashSHA1(content2, salt);

            Assert.NotEqual(result1, result2);
        }

        [Fact]
        public void BinaryToHex_WithNull_ReturnsNull()
        {
            var result = CryptoHelper.BinaryToHex(null);

            Assert.Null(result);
        }

        [Fact]
        public void BinaryToHex_WithEmptyArray_ReturnsEmptyString()
        {
            var result = CryptoHelper.BinaryToHex(new byte[0]);

            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void BinaryToHex_WithKnownBytes_ReturnsExpectedHex()
        {
            var bytes = new byte[] { 0x00, 0x0F, 0x10, 0xFF };

            var result = CryptoHelper.BinaryToHex(bytes);

            Assert.Equal("000F10FF", result);
        }

        [Theory]
        [InlineData(new byte[] { 0x00 }, "00")]
        [InlineData(new byte[] { 0x0F }, "0F")]
        [InlineData(new byte[] { 0x10 }, "10")]
        [InlineData(new byte[] { 0xFF }, "FF")]
        [InlineData(new byte[] { 0xAB, 0xCD }, "ABCD")]
        public void BinaryToHex_WithVariousBytes_ReturnsCorrectHex(byte[] input, string expected)
        {
            var result = CryptoHelper.BinaryToHex(input);

            Assert.Equal(expected, result);
        }

        [Theory]
        [AutoMoqData]
        public void BinaryToHex_WithRandomBytes_ReturnsCorrectLength(byte[] bytes)
        {
            var result = CryptoHelper.BinaryToHex(bytes);

            Assert.Equal(bytes.Length * 2, result.Length);
        }

        [Theory]
        [AutoMoqData]
        public void BinaryToHex_WithRandomBytes_ContainsOnlyHexCharacters(byte[] bytes)
        {
            var result = CryptoHelper.BinaryToHex(bytes);

            foreach (char c in result)
            {
                Assert.True((c >= '0' && c <= '9') || (c >= 'A' && c <= 'F'),
                    $"Character '{c}' is not a valid hex character");
            }
        }

        [Fact]
        public void HashSHA1_WithEmptyStrings_ReturnsValidHash()
        {
            var result = CryptoHelper.HashSHA1(string.Empty, string.Empty);

            Assert.NotNull(result);
            Assert.Equal(40, result.Length);
            // SHA1 of empty string
            Assert.Equal("DA39A3EE5E6B4B0D3255BFEF95601890AFD80709", result);
        }
    }
}
