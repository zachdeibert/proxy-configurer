function FindProxyForURL(url, host) {
    var opcodes = [
        "",
        "DIRECT",
        "PROXY",
        "SOCKS"
    ];
    var str = "";
    var nonce;
    var resolveUrl = host + ".proxyconfigurer.localhost";
    for (var i = 0; true; ++i) {
        var code = convert_addr(dnsResolve(resolveUrl));
        var opcode, port;
        if (nonce != null) {
            opcode = (code & 0xC0000000) >> 30;
            if (opcode < 0) {
                opcode += 4;
            }
            port = (code & 0x3FFF0000) >> 16;
            str += "; " + opcodes[opcode];
            if (opcode > 1) {
                str += " p" + i + ".n" + nonce + ".proxyconfigurer.localhost:" + port;
                ++i;
            } else {
                break;
            }
        } else {
            nonce = (code & 0xFFFF0000) >> 16;
            resolveUrl = "p.n" + nonce + ".proxyconfigurer.localhost";
        }
        code &= 0xFFFF;
        opcode = (code & 0xC000) >> 14;
        port = code & 0x3FFF;
        if (i > 0) {
            str += "; ";
        }
        str += opcodes[opcode];
        if (opcode > 1) {
            str += " p" + i + ".n" + nonce + ".proxyconfigurer.localhost:" + port;
        } else {
            break;
        }
    }
    return str;
}
