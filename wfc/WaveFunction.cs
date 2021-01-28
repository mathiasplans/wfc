using System.Collections;
using System.Collections.Generic;

public class WaveFunction {
    private uint[,][] constraints;
    public WaveFunctionEncoder wencoder;
    private int size;
    private void Initialize(Wave[] waves, uint adjacencies) {
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
        // For each wave in the observer
        this.wencoder.ForEachWave(observer, (order) => {
            // And the constraint with collapsable
            this.wencoder.AndInPlace(collapsable, this.constraints[adjacency, order]);
        });

        // Get the new entropy
        return this.wencoder.GetEntropy(collapsable);
    }

    private uint random(uint seed) {
        return seed ^ (seed >> 5) ^ (seed << 15) ^ (seed >> 20) ^ (seed << 7) ^ 0xDEADBEEF;
    }

    private uint randomSeed = 0xFFFF;

    private uint next() {
        this.randomSeed = random(this.randomSeed);
        return this.randomSeed;
    }

    /**
     * Get random bool. TODO: this is 50/50. Has to be so
     */
    private uint next(uint max) {
        return next() % max;
    }

    /**
     * Collapse a single wave function to a single wave
     * For now, we just take a random wave
     */
    public void Collapse(uint[] collapsable, uint entropy) {
        if (entropy == 0)
            return;

        // Get a random number in [0, entropy)
        uint rank = this.next(entropy);
        uint i = 0;

        // Variable where the order of the selected wave will be store
        int rankOrder = 0;

        // Iterate rank times.
        this.wencoder.ForEachWave(collapsable, (order) => {
            ++i;
            rankOrder = order;
            return i == rank;
        });

        // Set the bit at rankOrder and reset all the other bits
        this.wencoder.SelectWave(collapsable, rankOrder);
    }
}
