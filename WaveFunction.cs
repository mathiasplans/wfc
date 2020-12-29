using System.Collections;
using System.Collections.Generic;

public class WaveFunction {
    private uint size;
    private uint entropy;
    private Wave[] waves;

    public WaveFunction(Wave[] possibilitySpace) {
        this.size = (uint) possibilitySpace.Length;
        this.entropy = this.size;
        this.waves = possibilitySpace;
    }
}
