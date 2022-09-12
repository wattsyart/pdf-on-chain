using System.Collections;
using System.Text;
using Jering.Javascript.NodeJS;
using Nethereum.Hex.HexConvertors.Extensions;

const int maxChunkSize = 6144; // (6kB)

var buffer = await File.ReadAllBytesAsync("../../../../test/article.pdf");
await File.WriteAllBytesAsync("article.pdf", buffer);

var chunks = buffer.Chunk(maxChunkSize).ToList();
for (var i = 0; i < chunks.Count; i++)
{
    await File.WriteAllBytesAsync($"article_{i}.bin", chunks[i]);
}

const string packModule = @"
const pako = require('pako');
const fs = require('fs');
module.exports = (callback, index) => {
    const input = fs.readFileSync('./article_' + index + '.bin');
    const compressed = pako.deflateRaw(input, { level: 9 });
    fs.writeFileSync('./article_' + index + '.zip', compressed);
    var result = fs.readFileSync('./article_' + index + '.zip').length;
    callback(null, result);
}";

await GenerateArticleBanksAsync(chunks, packModule);
await GenerateArticleAsync(chunks);
await GenerateTestAsync(chunks);

async Task GenerateArticleBanksAsync(List<byte[]> bytesList, string s)
{
    var sb = new StringBuilder();

    sb.AppendLine("// SPDX-License-Identifier: UNLICENSED");
    sb.AppendLine();
    sb.AppendLine("pragma solidity ^0.8.13;");
    sb.AppendLine();
    sb.AppendLine("import \"./SSTORE2.sol\";");
    sb.AppendLine("import \"./BufferUtils.sol\";");
    sb.AppendLine();

    for (var i = 0; i < bytesList.Count; i++)
    {
        sb.AppendLine($"contract ArticleBank{i} {{");
        sb.AppendLine("    address immutable article;");
        sb.AppendLine("    uint immutable length;");

        var chunk = bytesList[i];
        var compressedLength = await StaticNodeJSService.InvokeFromStringAsync<int>(s, args: new object[] {i});

        var compressed = Ionic.Zlib.DeflateStream.CompressBuffer(chunk);
        if (compressed.Length != compressedLength)
            throw new InvalidOperationException("Failed to compress correctly!");

        var compressedChunkOnDisk = File.ReadAllBytes($"article_{i}.zip");

        var testDecompress = Ionic.Zlib.DeflateStream.UncompressBuffer(compressedChunkOnDisk);
        if (testDecompress.Length != chunk.Length)
            throw new InvalidOperationException("Failed to decompress correctly!");

        sb.AppendLine("    function get() external view returns (bytes memory) {");
        sb.AppendLine("        return BufferUtils.decompress(article, length);");
        sb.AppendLine("    }");
        sb.AppendLine("    constructor() {");
        sb.AppendLine($"        length = {chunk.Length};");
        sb.AppendLine($"        article = SSTORE2.write(hex\"{compressedChunkOnDisk.ToHex()}\");");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        sb.AppendLine();
    }

    sb.AppendLine();


    await File.WriteAllTextAsync("ArticleBanks.sol", sb.ToString());
}

async Task GenerateArticleAsync(ICollection bytesList)
{
    var sb = new StringBuilder();

    sb.AppendLine("// SPDX-License-Identifier: UNLICENSED");
    sb.AppendLine();
    sb.AppendLine("pragma solidity ^0.8.13;");
    sb.AppendLine();
    sb.AppendLine("import \"./Base64.sol\";");
    sb.AppendLine("import \"./DynamicBuffer.sol\";");
    sb.AppendLine("import \"./ArticleBanks.sol\";");
    sb.AppendLine();
    sb.AppendLine("contract Article {");
    sb.AppendLine();
    
    for (var i = 0; i < bytesList.Count; i++)
    {
        sb.AppendLine($"    ArticleBank{i} _{i};");
    }

    sb.AppendLine();
    sb.AppendLine("    struct AddressBank {");
    for (var i = 0; i < bytesList.Count; i++)
    {
        sb.AppendLine($"        address a{i};");
    }
    sb.AppendLine("    }");
    sb.AppendLine();
    sb.AppendLine("    constructor(AddressBank memory bank) {");
    for (var i = 0; i < bytesList.Count; i++)
    {
        sb.AppendLine($"        _{i} = ArticleBank{i}(bank.a{i});");
    }
    sb.AppendLine("    }");
    sb.AppendLine();
    sb.AppendLine("    struct AddressBuffers {");
    for (var i = 0; i < bytesList.Count; i++)
    {
        sb.AppendLine($"        bytes a{i};");
    }
    sb.AppendLine("    }");
    sb.AppendLine();
    sb.AppendLine("    function download() external view returns (string memory) {");

    var inner = new StringBuilder();
    inner.Append("        AddressBuffers memory b = AddressBuffers(");
    for (var i = 0; i < bytesList.Count; i++)
    {
        inner.Append($"_{i}.get()");
        if (i < bytesList.Count - 1)
        {
            inner.Append(", ");
        }
    }
    inner.Append(");");
    sb.AppendLine(inner.ToString());
    inner.Clear();
    inner.Append("        bytes memory buffer = DynamicBuffer.allocate(");
    for (var i = 0; i < bytesList.Count; i++)
    {
        inner.Append($"b.a{i}.length");
        if (i < bytesList.Count - 1)
        {
            inner.Append(" + ");
        }
    }
    inner.Append(");");
    sb.AppendLine(inner.ToString());
    sb.AppendLine();
    for (var i = 0; i < bytesList.Count; i++)
    {
        sb.AppendLine($"        DynamicBuffer.appendUnchecked(buffer, b.a{i});");
    }
    sb.AppendLine();
    sb.AppendLine("        return string(abi.encodePacked('data:application/pdf;base64,', Base64.encode(buffer, buffer.length)));");
    sb.AppendLine("    }");
    sb.AppendLine("}");

    await File.WriteAllTextAsync("Article.sol", sb.ToString());
}

async Task GenerateTestAsync(ICollection bytesList)
{
    var sb = new StringBuilder();
    sb.AppendLine("const hre = require(\"hardhat\");");
    sb.AppendLine();
    sb.AppendLine("describe(\"Deployments\", function () {");
    sb.AppendLine("    it(\"deploys all contracts\", async function () {");

    for (var i = 0; i < bytesList.Count; i++)
    {
        sb.AppendLine($"        var a{i} = await hre.ethers.getContractFactory(\"ArticleBank{i}\");");
        sb.AppendLine($"        var a{i}D = await a{i}.deploy();");
        sb.AppendLine($"        await a{i}D.deployed();");

        if (i < bytesList.Count - 1)
            sb.AppendLine();
    }

    sb.AppendLine("        var contract = await hre.ethers.getContractFactory(\"Article\");");
    sb.AppendLine("        var deployed = await contract.deploy({");

    for (var i = 0; i < bytesList.Count; i++)
    {
        sb.Append($"            a{i}: a{i}D.address");
        if (i < bytesList.Count - 1)
            sb.Append(", ");
        sb.AppendLine();
    }
    sb.AppendLine("        });");
    sb.AppendLine();
    sb.AppendLine("        await deployed.deployed();");
    sb.AppendLine("        var article = await deployed.download();");
    sb.AppendLine("        console.log(article);");
    sb.AppendLine("    });");
    sb.AppendLine("});");

    await File.WriteAllTextAsync("Article.js", sb.ToString());
}