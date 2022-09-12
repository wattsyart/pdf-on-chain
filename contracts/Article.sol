// SPDX-License-Identifier: UNLICENSED

pragma solidity ^0.8.13;

import "./Base64.sol";
import "./DynamicBuffer.sol";
import "./ArticleBanks.sol";

contract Article {

    ArticleBank0 _0;
    ArticleBank1 _1;
    ArticleBank2 _2;

    struct AddressBank {
        address a0;
        address a1;
        address a2;
    }

    constructor(AddressBank memory bank) {
        _0 = ArticleBank0(bank.a0);
        _1 = ArticleBank1(bank.a1);
        _2 = ArticleBank2(bank.a2);
    }

    struct AddressBuffers {
        bytes a0;
        bytes a1;
        bytes a2;
    }

    function download() external view returns (string memory) {
        AddressBuffers memory b = AddressBuffers(_0.get(), _1.get(), _2.get());
        bytes memory buffer = DynamicBuffer.allocate(b.a0.length + b.a1.length + b.a2.length);

        DynamicBuffer.appendUnchecked(buffer, b.a0);
        DynamicBuffer.appendUnchecked(buffer, b.a1);
        DynamicBuffer.appendUnchecked(buffer, b.a2);

        return string(abi.encodePacked('data:application/pdf;base64,', Base64.encode(buffer, buffer.length)));
    }
}
