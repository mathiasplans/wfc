using System.Collections;
using System.Collections.Generic;
using System;

public class WaveFunction {
    private uint[,][] constraints;
    public WaveFunctionEncoder wencoder;
    private int size;
    private uint adjacencies;

    private void Initialize(Wave[] waves, uint adjacencies) {
        this.adjacencies = adjacencies;

        // Encode the wave function
        this.wencoder = new WaveFunctionEncoder(waves);
        this.size = wencoder.GetSize();

        // Now create the rules (constraints guide)
        this.constraints = new uint[adjacencies, this.size][];

        for (uint i = 0; i < this.size; ++i) {
            Wave subject = waves[i];
            int order = this.wencoder.GetOrder(subject);

            // Get the possible adjacencies
            for (uint j = 0; j < adjacencies; ++j) {
                Wave[] consts = subject.GetConstraints(j);

                // Initialize the waveFunction
                this.constraints[j, order] = new uint[this.wencoder.GetEncodeSize()];
                foreach (Wave wave in consts) {
                    this.wencoder.SetWave(this.constraints[j, order], wave);
                }
            }
        }

        uint fixedTotal = 0U;

        // Fix the adjacencies
        // Iterate through all the pairs of wave functions
        for (int i = 0; i < this.wencoder.waves.Length - 1; ++i) {
            // Get the i-th wavefunction
            uint[] a = this.wencoder.GetSolo(this.wencoder.waves[i]);

            for (int j = i + 1; j < this.wencoder.waves.Length; ++j) {
                // Get the j-th wavefunction
                uint[] b = this.wencoder.GetSolo(this.wencoder.waves[j]);

                // Iterate all the adjacencies
                // TODO: use adjacencies
                for (uint k = 0; k < this.adjacencies; ++k) {
                    // Also get the opposite side
                    uint l = (k + this.adjacencies / 2) % this.adjacencies;

                    // Get the i-th constraint
                    uint[] acons = this.constraints[k, i];

                    // Get the j-th constraint
                    uint[] bcons = this.constraints[l, j];

                    // Fix all the conflicts
                    bool aok = false, bok = false;
                    for (uint m = 0; m < this.wencoder.GetEncodeSize(); ++m) {
                        aok |= (a[m] & bcons[m]) != 0;
                        bok |= (b[m] & acons[m]) != 0;
                    }
                    
                    // If the relation is asymmetrical
                    if (aok != bok) {
                        fixedTotal += 1;
                        this.wencoder.SetWave(this.constraints[k, i], j);
                        this.wencoder.SetWave(this.constraints[l, j], i);
                    }
                }
            }
        }

        Console.WriteLine("Fixed " + fixedTotal + " adjacensies");
    }

    public WaveFunction(Wave[] waves) {
        this.Initialize(waves, 1);
    }

    public WaveFunction(Wave[] waves, uint adjacencies) {
        this.Initialize(waves, adjacencies);
    }

     /**
     * Collapse the possibility space a bit.
     * Returns a new entropy of the collapsable
     * This function presumes that the two provided Wave functions are adjacent to each other
     *
     * observer is the wavefunction that wants to propagate its state,
     * collapsable is the wavefunction that changes due to changes from the observer
     */
    public uint Collapse(uint[] observer, uint[] collapsable) {
        return this.Collapse(observer, collapsable, 0);
    }

    /**
     * Same as regular collapse, but takes argument for special adjacency.
     * For example, one can define four adjacencies: north - 0, east - 1, south - 2, west - 3.
     * This argument will be taken into account on the collapse.
     *
     * observer is the wavefunction that wants to propagate its state,
     * collapsable is the wavefunction that changes due to changes from the observer
     */
    public uint Collapse(uint[] observer, uint[] collapsable, uint adjacency) {
        // Console.WriteLine("b" + Convert.ToString(observer[0], 2));
        // Console.WriteLine("b" + Convert.ToString(collapsable[0], 2));

        uint[] mask = this.wencoder.GetEmpty();
        // For each wave in the observer
        this.wencoder.ForEachWave(observer, (order) => {
            // Or all the constraints together
            this.wencoder.OrInPlace(mask, this.constraints[adjacency, order]);
        });

        // Console.WriteLine("Collapse:");
        // Console.WriteLine(this.wencoder.FormatWave(mask));
        // Console.WriteLine("&");
        // Console.WriteLine(this.wencoder.FormatWave(collapsable));

        // Now and the collapsable with all the constraints
        this.wencoder.AndInPlace(collapsable, mask);

        // Console.WriteLine("=");
        // Console.WriteLine(this.wencoder.FormatWave(collapsable));

        // Get the new entropy
        return this.wencoder.GetEntropy(collapsable);
    }

    // private uint random(uint seed) {
    //     return seed ^ (seed >> 5) ^ (seed << 15) ^ (seed >> 20) ^ (seed << 7) ^ 0xDEADBEEF;
    // }

    // private uint randomSeed = 0x9999;
    Random rnd = new Random();

    private uint next() {
        // this.randomSeed = random(this.randomSeed);
        // return this.randomSeed;
        return (uint) rnd.Next();
    }

    /**
     * Get random bool. TODO: this is 50/50. Has to be so
     */
    private uint next(uint max) {
        return next() % max;
    }

    private float next01() {
        return (float) rnd.NextDouble();
    }

    /**
     * Collapse a single wave function to a single wave
     * For now, we just take a random wave
     */
    public void Collapse(uint[] collapsable) {
        // Set the bit at rankOrder and reset all the other bits
        this.wencoder.WeightedSelect(collapsable, next01());
    }
}
