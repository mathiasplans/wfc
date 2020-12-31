using System.Collections;
using System.Collections.Generic;

public class Wave {
    private uint adjacencies;
    private Wave[][] constraints;
    public readonly string name;

    public Wave(uint adjacencies, string name) {
        this.adjacencies = adjacencies;
        this.constraints = new Wave[adjacencies][];
        this.name = name;
    }

    public void AddConstraints(uint side, Wave[] waves) {
        this.constraints[side] = waves;
    }

    public Wave[] GetConstraints(uint i) {
        return this.constraints[i];
    }

    public Wave ShiftedWave() {
        Wave newWave = new Wave(this.adjacencies, this.name);
        newWave.AddConstraints(0, this.constraints[3]);
        newWave.AddConstraints(1, this.constraints[0]);
        newWave.AddConstraints(2, this.constraints[1]);
        newWave.AddConstraints(3, this.constraints[2]);

        return newWave;
    }
}
