// SPDX-License-Identifier: UNLICENSED

pragma solidity ^0.8.13;

import "./InflateLib.sol";
import "./SSTORE2.sol";
import "hardhat/console.sol";

library BufferUtils {

    error FailedToDecompress(uint errorCode);
    error InvalidDecompressionLength(uint expected, uint actual);

    function decompress(address compressed, uint256 decompressedLength)
        internal
        view
        returns (bytes memory)
    {
        (InflateLib.ErrorCode code, bytes memory buffer) = InflateLib.puff(
            SSTORE2.read(compressed),
            decompressedLength
        );
        if (code != InflateLib.ErrorCode.ERR_NONE) {
            console.log("FailedToDecompress(%s)", uint(code));
            revert FailedToDecompress(uint256(code));
        }            
        if (buffer.length != decompressedLength)
        {
            console.log("InvalidDecompressionLength(%s)", decompressedLength, buffer.length);
            revert InvalidDecompressionLength(
                decompressedLength,
                buffer.length
            );
        }            
        return buffer;
    }
}