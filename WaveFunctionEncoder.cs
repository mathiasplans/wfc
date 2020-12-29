using System.Collections;
using System.Collections.Generic;
using System;

public class WaveFunctionEncoder {
    private int size;
    private uint[] encoded;
    private uint[,][] constraints;
    private int encodeSize;
    private uint spareMask = 0x00000000;

    private Dictionary<Wave, int> waveOrder;
    private Dictionary<int, Wave> orderWave;

    /**
     *
     */
    public WaveFunctionEncoder(Wave[] waves, uint adjacencies) {
        this.size = waves.Length;

        this.encodeSize = this.size / 8;
        if (this.size % 32U != 0U) {
            this.encodeSize += 1;
            this.spareMask = ~(~0U << (this.size % 32));
        }

        // Mapping between the wave order and wave
        this.waveOrder = new Dictionary<Wave, int>();
        this.orderWave = new Dictionary<int, Wave>();

        for (int i = 0; i < this.size; ++i) {
            this.waveOrder.Add(waves[i], i);
            this.orderWave.Add(i, waves[i]);
        }

        // Create the encoded array
        this.encoded = new uint[this.encodeSize];

        // Create a full encoding
        for (uint i = 0; i < this.encodeSize; ++i) {
            this.encoded[i] = ~0U;
        }

        // Last word
        this.encoded[this.encodeSize - 1] ^= this.spareMask;

        // Now create the rules (constraints guide)
        this.constraints = new uint[adjacencies, this.encodeSize][];

        for (uint i = 0; i < this.size; ++i) {
            Wave subject = waves[i];
            int order = this.waveOrder[subject];

            // Get the possible adjacencies
            for (uint j = 0; j < adjacencies; ++j) {
                Wave[] consts = subject.GetConstraints(j);

                // Initialize the waveFunction
                this.constraints[j, order] = new uint[this.encodeSize];

                foreach (Wave wave in consts) {
                    this.SetWave(this.constraints[j, order], wave);
                }
            }
        }
    }

    /**
     * Full possibility space, aka WaveFunction
     */
    public uint[] GetPossibilitySpace() {
        uint[] r = new uint[this.encodeSize];
        Array.Copy(this.encoded, r, this.encodeSize);
        return r;
    }

    /**
     * Set the wave bit
     */
    private void SetWave(uint[] waveFunction, int order) {
        waveFunction[order / 32] |= 1U << (order % 32);
    }

    /**
     * Add the wave to the wave function
     */
    public void SetWave(uint[] waveFunction, Wave wave) {
        int order = this.waveOrder[wave];
        this.SetWave(waveFunction, order);
    }

    /**
     * Reset the wave bit
     */
    private void ResetWave(uint[] waveFunction, int order) {
        waveFunction[order / 32] &= ~(1U << (order % 32));
    }

    /**
     * Remove the wave from the wave function
     */
    public void ResetWave(uint[] waveFunction, Wave wave) {
        int order = this.waveOrder[wave];
        this.ResetWave(waveFunction, order);
    }

    /**
     * Get the full possibility space with some waves absent
     */
    public uint[] GetPossibilitySpace(Wave[] exclude) {
        // Get a fresh copy
        uint[] r = this.GetPossibilitySpace();

        // Now exclude the waves
        foreach (Wave wave in exclude) {
            // Get the order
            int order = this.waveOrder[wave];

            // Clear the bit
            this.ResetWave(r, order);
        }

        return r;
    }

    /**
     * Get a possibility space with only one possibility (entropy is 0)
     */
    public uint[] GetSolo(Wave wave) {
        // Byte has to be same length as full space
        uint[] r = new uint[this.encodeSize];

        // Get the order
        int order = this.waveOrder[wave];

        // Set the bit
        this.SetWave(r, order);

        return r;
    }

    /**
     * Merge two wave functions together
     */
    public uint[] MergeWaveFunctions(uint[] space1, uint[] space2) {
        uint[] r = new uint[this.encodeSize];

        for (uint i = 0; i < this.encodeSize; ++i) {
            r[i] = space1[i] | space2[i];
        }

        return r;
    }

    /**
     * Merge two wave functions together. The operation is done
     * on waveFunction in-place.
     */
    private void MergeInPlace(uint[] waveFunction, uint[] merger) {
        for (uint i = 0; i < this.encodeSize; ++i) {
            waveFunction[i] |= merger[i];
        }
    }

    /**
     * Remove one waveFunction from another. The operation is done
     * on waveFunction in-place.
     */
    private void RemoveInPlace(uint[] waveFunction, uint[] remover) {
        for (uint i = 0; i < this.encodeSize; ++i) {
            waveFunction[i] ^= waveFunction[i] & remover[i];
        }
    }

    /**
     * And the wave functions together. The operation is done
     * on waveFunction in-place.
     */
    private void AndInPlace(uint[] waveFunction, uint[] ander) {
        for (uint i = 0; i < this.encodeSize; ++i) {
            waveFunction[i] &= ander[i];
        }
    }

    /**
     * Get the entropy of the wave function. It counts the number of ones
     * in the encoded uint array and then returns one less than that (entropy 1 should
     * be minimum). The user has to make sure not to give an empty wave function as
     * the argument, since entropy below 0 is not supported.
     */
    public uint GetEntropy(uint[] waveFunction) {
        uint entropy = 0;
        foreach (byte slot in waveFunction) {
            entropy += (uint) WFCPopCount.u32(slot);
        }

        return entropy - 1;
    }

    /**
     * Iteration function. Apply the action for each wave in the waveFunction
     */
    private void ForEachWave(uint[] waveFunction, Action<int> action) {
        uint order = 0U;

        // Iterate slotwise
        foreach (uint s in waveFunction) {
            uint slot = s;
            // Iterate bitwise
            for (uint i = 0U; i < 32U; ++i) {
                // End if slot is empty
                if (slot == 0U)
                    break;

                // If current bit is 1, call the function
                if ((slot & 1U) == 1U) {
                    // Call the callback
                    action((int) (order + i));
                }

                // Shift the slot
                slot >>= 1;
            }

            // Increment order
            order += 8U;
        }
    }

    /**
     * Decode the wave function to get the actual waves
     */
    public Wave[] GetWaves(uint[] waveFunction) {
        // Create the list for waves
        List<Wave> wavesInFunction = new List<Wave>();

        // For each wave in wave function
        this.ForEachWave(waveFunction, (order) => {
            wavesInFunction.Add(this.orderWave[order]);
        });

        // Convert to array
        return wavesInFunction.ToArray();
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
        // For each wave in the observer
        this.ForEachWave(observer, (order) => {
            // And the constraint with collapsable
            this.AndInPlace(collapsable, constraints[adjacency, order]);
        });

        // Get the new entropy
        return this.GetEntropy(collapsable);
    }
}
