require("@nomicfoundation/hardhat-toolbox");

/**
 * @type import('hardhat/config').HardhatUserConfig
 */
 module.exports = {
  defaultNetwork: "hardhat",
  networks: {
    hardhat: {
      gas: "auto",
      blockGasLimit: 900_000_000_000,
      initialBaseFeePerGas: 0,
      timeout: 43200000
    },
    privatenode: {
      url: "http://127.0.0.1:8545",
      accounts: ["0xb0a587bc9681a7333763f84c3b90a4d58bd01b5fb0635ac16187f6f55e792a57"],
      gasPrice: "auto",
      gas: "auto",
      timeout: 43200000
    },
  },
  solidity: {
    version: "0.8.13",
    settings: {
      optimizer: {
        enabled: true,
        runs: 1
      }
    }
  },
  mocha: {
    timeout: 2000000000
  },
  gasReporter: {
    enabled: true
  }
};
