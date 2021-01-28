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

    public void AddConstraints(Wave[] waves) {
        this.constraints[0] = waves;
    }

    public Wave[] GetConstraints(uint i) {
        return this.constraints[i];
    }

    public uint GetSides() {
        return (uint) this.constraints.Length;
    }

    public Wave ShiftedWave() {
        Wave newWave = new Wave(this.adjacencies, this.name);
        for (uint i = 0; i < this.GetSides(); ++i) {
            newWave.AddConstraints((i + 1) % this.GetSides(), this.constraints[i]);
        }

        return newWave;
    }
}
