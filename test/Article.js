const hre = require("hardhat");

describe("Deployments", function () {
    it("deploys all contracts", async function () {
        var a0 = await hre.ethers.getContractFactory("ArticleBank0");
        var a0D = await a0.deploy();
        await a0D.deployed();

        var a1 = await hre.ethers.getContractFactory("ArticleBank1");
        var a1D = await a1.deploy();
        await a1D.deployed();

        var a2 = await hre.ethers.getContractFactory("ArticleBank2");
        var a2D = await a2.deploy();
        await a2D.deployed();
        var contract = await hre.ethers.getContractFactory("Article");
        var deployed = await contract.deploy({
            a0: a0D.address, 
            a1: a1D.address, 
            a2: a2D.address
        });

        await deployed.deployed();
        var article = await deployed.download();
        console.log(article);
    });
});
