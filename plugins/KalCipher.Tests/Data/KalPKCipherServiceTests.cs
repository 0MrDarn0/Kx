// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Sdk.Cipher;

using Moq;

namespace KalCipher.Data.Tests;

/// <summary>
/// Unit tests for <see cref="KalPKCipherService"/>.
/// </summary>
public class KalPKCipherServiceTests {
    /// <summary>
    /// Tests that LoadArchiveAsync throws ArgumentNullException when filePath is null.
    /// </summary>
    [Fact]
    public async Task LoadArchiveAsync_NullFilePath_ThrowsArgumentNullException() {
        // Arrange
        var service = new KalPKCipherService();
        string? filePath = null;
        string password = "testPassword";

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await service.LoadArchiveAsync(filePath!, password));
        Assert.Equal("filePath", exception.ParamName);
    }

    /// <summary>
    /// Tests that LoadArchiveAsync throws ArgumentNullException when password is null.
    /// </summary>
    [Fact]
    public async Task LoadArchiveAsync_NullPassword_ThrowsArgumentNullException() {
        // Arrange
        var service = new KalPKCipherService();
        string filePath = "test.pk";
        string? password = null;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await service.LoadArchiveAsync(filePath, password!));
        Assert.Equal("password", exception.ParamName);
    }

    /// <summary>
    /// Tests that LoadArchiveAsync throws an exception when filePath is an empty string.
    /// The exception occurs when attempting to open a file with an empty path.
    /// </summary>
    [Fact]
    public async Task LoadArchiveAsync_EmptyFilePath_ThrowsException() {
        // Arrange
        var service = new KalPKCipherService();
        string filePath = string.Empty;
        string password = "testPassword";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await service.LoadArchiveAsync(filePath, password));
    }

    /// <summary>
    /// Tests that LoadArchiveAsync throws an exception when filePath contains only whitespace.
    /// The exception occurs when attempting to open a file with a whitespace-only path.
    /// </summary>
    [Fact]
    public async Task LoadArchiveAsync_WhitespaceFilePath_ThrowsException() {
        // Arrange
        var service = new KalPKCipherService();
        string filePath = "   ";
        string password = "testPassword";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await service.LoadArchiveAsync(filePath, password));
    }

    /// <summary>
    /// Tests that LoadArchiveAsync throws FileNotFoundException when the specified file does not exist.
    /// </summary>
    [Fact]
    public async Task LoadArchiveAsync_NonExistentFile_ThrowsFileNotFoundException() {
        // Arrange
        var service = new KalPKCipherService();
        string filePath = "nonexistent_file_that_does_not_exist_12345.pk";
        string password = "testPassword";

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(
            async () => await service.LoadArchiveAsync(filePath, password));
    }

    /// <summary>
    /// Tests that LoadArchiveAsync throws an exception when filePath contains invalid path characters.
    /// </summary>
    [Fact]
    public async Task LoadArchiveAsync_InvalidPathCharacters_ThrowsException() {
        // Arrange
        var service = new KalPKCipherService();
        string filePath = "invalid<>|path?.pk";
        string password = "testPassword";

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(
            async () => await service.LoadArchiveAsync(filePath, password));
    }

    /// <summary>
    /// Tests that LoadArchiveAsync throws an exception when password is an empty string.
    /// The behavior depends on the underlying ZIP library's handling of empty passwords.
    /// </summary>
    [Fact]
    public async Task LoadArchiveAsync_EmptyPassword_ThrowsException() {
        // Arrange
        var service = new KalPKCipherService();
        string filePath = "nonexistent_file_12345.pk";
        string password = string.Empty;

        // Act & Assert
        // The file doesn't exist, so FileNotFoundException is expected first
        await Assert.ThrowsAsync<FileNotFoundException>(
            async () => await service.LoadArchiveAsync(filePath, password));
    }

    /// <summary>
    /// Tests that LoadArchiveAsync throws an exception when password contains only whitespace.
    /// </summary>
    [Fact]
    public async Task LoadArchiveAsync_WhitespacePassword_ThrowsException() {
        // Arrange
        var service = new KalPKCipherService();
        string filePath = "nonexistent_file_12345.pk";
        string password = "   ";

        // Act & Assert
        // The file doesn't exist, so FileNotFoundException is expected first
        await Assert.ThrowsAsync<FileNotFoundException>(
            async () => await service.LoadArchiveAsync(filePath, password));
    }

    /// <summary>
    /// Tests that EncryptDat throws ArgumentNullException when content parameter is null.
    /// </summary>
    [Fact]
    public void EncryptDat_NullContent_ThrowsArgumentNullException() {
        // Arrange
        var service = new KalPKCipherService();
        string? content = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => service.EncryptDat(content!));
    }

    /// <summary>
    /// Tests that EncryptDat returns a valid byte array for an empty string input.
    /// </summary>
    [Fact]
    public void EncryptDat_EmptyString_ReturnsValidByteArray() {
        // Arrange
        var service = new KalPKCipherService();
        var content = string.Empty;

        // Act
        var result = service.EncryptDat(content);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<byte[]>(result);
    }

    /// <summary>
    /// Tests that EncryptDat returns a non-null byte array for a simple string input.
    /// </summary>
    [Fact]
    public void EncryptDat_SimpleString_ReturnsNonNullByteArray() {
        // Arrange
        var service = new KalPKCipherService();
        var content = "test";

        // Act
        var result = service.EncryptDat(content);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<byte[]>(result);
        Assert.NotEmpty(result);
    }

    /// <summary>
    /// Tests that EncryptDat handles whitespace-only strings correctly.
    /// </summary>
    /// <param name="content">The whitespace string to test.</param>
    [Theory]
    [InlineData(" ")]
    [InlineData("  ")]
    [InlineData("\t")]
    [InlineData("\n")]
    [InlineData("\r\n")]
    [InlineData("   \t\n   ")]
    public void EncryptDat_WhitespaceString_ReturnsValidByteArray(string content) {
        // Arrange
        var service = new KalPKCipherService();

        // Act
        var result = service.EncryptDat(content);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<byte[]>(result);
    }

    /// <summary>
    /// Tests that EncryptDat handles strings with special characters correctly.
    /// </summary>
    /// <param name="content">The string with special characters to test.</param>
    [Theory]
    [InlineData("!@#$%^&*()")]
    [InlineData("<html>test</html>")]
    [InlineData("test\0null")]
    [InlineData("line1\nline2")]
    [InlineData(@"tab\here")]
    [InlineData("emoji: 😀🎉")]
    [InlineData("unicode: \u00A9\u00AE")]
    [InlineData("quotes: \"'`")]
    [InlineData("backslash: \\")]
    public void EncryptDat_SpecialCharacters_ReturnsValidByteArray(string content) {
        // Arrange
        var service = new KalPKCipherService();

        // Act
        var result = service.EncryptDat(content);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<byte[]>(result);
        Assert.NotEmpty(result);
    }

    /// <summary>
    /// Tests that EncryptDat handles very long strings correctly.
    /// </summary>
    [Fact]
    public void EncryptDat_VeryLongString_ReturnsValidByteArray() {
        // Arrange
        var service = new KalPKCipherService();
        var content = new string('a', 100000);

        // Act
        var result = service.EncryptDat(content);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<byte[]>(result);
        Assert.NotEmpty(result);
    }

    /// <summary>
    /// Tests that EncryptDat returns different results for different inputs.
    /// </summary>
    [Fact]
    public void EncryptDat_DifferentInputs_ReturnsDifferentResults() {
        // Arrange
        var service = new KalPKCipherService();
        var content1 = "test1";
        var content2 = "test2";

        // Act
        var result1 = service.EncryptDat(content1);
        var result2 = service.EncryptDat(content2);

        // Assert
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.NotEqual(result1, result2);
    }

    /// <summary>
    /// Tests that DecryptDat successfully processes an empty byte array without throwing exceptions.
    /// </summary>
    [Fact]
    public void DecryptDat_EmptyByteArray_ReturnsString() {
        // Arrange
        var service = new KalPKCipherService();
        byte[] data = Array.Empty<byte>();

        // Act
        var result = service.DecryptDat(data);

        // Assert
        Assert.NotNull(result);
    }

    /// <summary>
    /// Tests that DecryptDat successfully processes byte arrays of various lengths.
    /// Input conditions: byte arrays with different sizes and content.
    /// Expected result: returns a non-null string for valid byte arrays.
    /// </summary>
    /// <param name="data">The byte array to decrypt.</param>
    [Theory]
    [InlineData(new byte[] { 0x00 })]
    [InlineData(new byte[] { 0xFF })]
    [InlineData(new byte[] { 0x01, 0x02, 0x03 })]
    [InlineData(new byte[] { 0x00, 0x00, 0x00 })]
    [InlineData(new byte[] { 0xFF, 0xFF, 0xFF })]
    public void DecryptDat_ValidByteArray_ReturnsNonNullString(byte[] data) {
        // Arrange
        var service = new KalPKCipherService();

        // Act
        var result = service.DecryptDat(data);

        // Assert
        Assert.NotNull(result);
    }

    /// <summary>
    /// Tests that DecryptDat handles a large byte array correctly.
    /// Input conditions: byte array with 10000 elements.
    /// Expected result: returns a non-null string without throwing exceptions.
    /// </summary>
    [Fact]
    public void DecryptDat_LargeByteArray_ReturnsString() {
        // Arrange
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        var service = new KalPKCipherService();
        byte[] data = new byte[10000];
        for (int i = 0; i < data.Length; i++) {
            data[i] = (byte)(i % 256);
        }

        // Act
        var result = service.DecryptDat(data);

        // Assert
        Assert.NotNull(result);
    }

    /// <summary>
    /// Tests that DecryptDat handles byte arrays containing all possible byte values (0-255).
    /// Input conditions: byte array containing each byte value from 0 to 255.
    /// Expected result: returns a non-null string representing the decrypted content.
    /// </summary>
    [Fact]
    public void DecryptDat_AllByteValues_ReturnsString() {
        // Arrange
        var service = new KalPKCipherService();
        byte[] data = new byte[256];
        for (int i = 0; i < 256; i++) {
            data[i] = (byte)i;
        }

        // Act
        var result = service.DecryptDat(data);

        // Assert
        Assert.NotNull(result);
    }

    /// <summary>
    /// Tests that SaveArchiveAsync calls SaveAsync on the provided archive.
    /// Input: Valid IPkArchive mock.
    /// Expected: SaveAsync is invoked exactly once on the archive.
    /// </summary>
    [Fact]
    public async Task SaveArchiveAsync_ValidArchive_CallsSaveAsyncOnArchive() {
        // Arrange
        var archiveMock = new Mock<IPkArchive>();
        archiveMock.Setup(a => a.SaveAsync()).Returns(Task.CompletedTask);
        var service = new KalPKCipherService();

        // Act
        await service.SaveArchiveAsync(archiveMock.Object);

        // Assert
        archiveMock.Verify(a => a.SaveAsync(), Times.Once);
    }

    /// <summary>
    /// Tests that SaveArchiveAsync throws ArgumentNullException when archive is null.
    /// Input: Null archive parameter.
    /// Expected: ArgumentNullException or NullReferenceException is thrown.
    /// </summary>
    [Fact]
    public async Task SaveArchiveAsync_NullArchive_ThrowsException() {
        // Arrange
        var service = new KalPKCipherService();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await service.SaveArchiveAsync(null!));
    }

    /// <summary>
    /// Tests that SaveArchiveAsync propagates exceptions thrown by SaveAsync.
    /// Input: Archive that throws InvalidOperationException during SaveAsync.
    /// Expected: InvalidOperationException is propagated to caller.
    /// </summary>
    [Fact]
    public async Task SaveArchiveAsync_SaveAsyncThrowsException_PropagatesException() {
        // Arrange
        var archiveMock = new Mock<IPkArchive>();
        var expectedException = new InvalidOperationException("Save failed");
        archiveMock.Setup(a => a.SaveAsync()).ThrowsAsync(expectedException);
        var service = new KalPKCipherService();

        // Act & Assert
        var actualException = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await service.SaveArchiveAsync(archiveMock.Object));
        Assert.Same(expectedException, actualException);
    }

    /// <summary>
    /// Tests that SaveArchiveAsync completes successfully when SaveAsync completes.
    /// Input: Archive with successful SaveAsync completion.
    /// Expected: Task completes without exception.
    /// </summary>
    [Fact]
    public async Task SaveArchiveAsync_SaveAsyncCompletes_CompletesSuccessfully() {
        // Arrange
        var archiveMock = new Mock<IPkArchive>();
        archiveMock.Setup(a => a.SaveAsync()).Returns(Task.CompletedTask);
        var service = new KalPKCipherService();

        // Act
        await service.SaveArchiveAsync(archiveMock.Object);

        // Assert - no exception thrown, task completed successfully
        archiveMock.Verify(a => a.SaveAsync(), Times.Once);
    }

    /// <summary>
    /// Tests that SaveArchiveAsync propagates TaskCanceledException when SaveAsync is cancelled.
    /// Input: Archive that throws TaskCanceledException during SaveAsync.
    /// Expected: TaskCanceledException is propagated to caller.
    /// </summary>
    [Fact]
    public async Task SaveArchiveAsync_SaveAsyncCancelled_PropagatesTaskCanceledException() {
        // Arrange
        var archiveMock = new Mock<IPkArchive>();
        archiveMock.Setup(a => a.SaveAsync()).ThrowsAsync(new TaskCanceledException());
        var service = new KalPKCipherService();

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(async () =>
            await service.SaveArchiveAsync(archiveMock.Object));
    }
}
