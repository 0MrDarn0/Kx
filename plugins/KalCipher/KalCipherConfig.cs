// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace KalCipher.Config;



public class KalCipherConfig {
    public PkConfig Pk { get; set; } = new();
}

public class PkConfig {
    public int Codepage { get; set; } = 949;
    public string Password { get; set; } = "JKSYEHAB#9052";
    public int CKey { get; set; } = 8;
    public int EKey { get; set; } = 4;

}
