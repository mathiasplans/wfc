using System.Collections;
using System.Collections.Generic;
using System;

public class WaveFunctionEncoder {
    private int size;
    private uint[] encoded;
    private int encodeSize;

    private Dictionary<Wave, int> waveOrder;
    private Dictionary<int, Wave> orderWave;

    private void Initialize(Wave[] waves) {
        this.size = waves.Length;

        // How many words have to be used
        uint spareMask = 0;
        this.encodeSize = this.size / 32;
        if (this.size % 32 != 0) {
            this.encodeSize += 1;
            spareMask = ~0U << (this.size % 32);
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
        this.encoded[this.encodeSize - 1] ^= spareMask;
    }

    /**
     * Constructor for naive wave function collapse. The context of adjacency (e.g direction)
     * does not matter. This is useful when running the wave function collapse algorithm on a
     * graph.
     */
    public WaveFunctionEncoder(Wave[] waves) {
        this.Initialize(waves);
    }

    /**
     * Get the number of possible waves
     */
    public int GetSize() {
        return this.size;
    }

    /**
     * Size of the encoded wave in 32-bit words
     */
    public int GetEncodeSize() {
        return this.encodeSize;
    }

    /**
     * Get the index of the wave in the encoding
     */
    public int GetOrder(Wave wave) {
        return this.waveOrder[wave];
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
     * Get the possibility space with some waves
     */
    public uint[] GetPossibilitySpace(Wave[] include) {
        // Get a fresh copy
        uint[] r = new uint[this.encodeSize];

        // Now exclude the waves
        foreach (Wave wave in include) {
            // Get the order
            int order = this.waveOrder[wave];

            // Clear the bit
            this.SetWave(r, order);
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
     * Produces an empty encoding with entropy -1
     */
    public uint[] GetEmpty() {
        return new uint[this.encodeSize];
    }
    

    /**
     * Set the wave bit
     */
    public void SetWave(uint[] waveFunction, int order) {
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
    public void ResetWave(uint[] waveFunction, int order) {
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
     * Sets a specific bit and resets everything else
     */
    public void SelectWave(uint[] waveFunction, int order) {
        for (uint i = 0; i < waveFunction.Length; ++i) {
            waveFunction[i] = 0;
        }

        this.SetWave(waveFunction, order);
    }

    /**
     * Set only one specified wave. Every other wave will be removed.
     */
    public void SelectWave(uint[] waveFunction, Wave wave) {
        int order = this.waveOrder[wave];
        this.SelectWave(waveFunction, order);
    }

    /**
     * Merge two wave functions together
     */
    public uint[] MergeWaveFunctions(uint[] space1, uint[] space2) {
        uint[] r = new uint[space1.Length];

        for (uint i = 0; i < space1.Length; ++i) {
            r[i] = space1[i] | space2[i];
        }

        return r;
    }

    /**
     * Merge two wave functions together. The operation is done
     * on waveFunction in-place.
     */
    public void MergeInPlace(uint[] waveFunction, uint[] merger) {
        for (uint i = 0; i < waveFunction.Length; ++i) {
            waveFunction[i] |= merger[i];
        }
    }

    /**
     * Remove one waveFunction from another. The operation is done
     * on waveFunction in-place.
     */
    public void RemoveInPlace(uint[] waveFunction, uint[] remover) {
        for (uint i = 0; i < waveFunction.Length; ++i) {
            waveFunction[i] ^= waveFunction[i] & remover[i];
        }
    }

    /**
     * And the wave functions together. The operation is done
     * on waveFunction in-place.
     */
    public void AndInPlace(uint[] waveFunction, uint[] ander) {
        for (uint i = 0; i < waveFunction.Length; ++i) {
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
        foreach (uint slot in waveFunction) {
            entropy += (uint) WFCPopCount.u32(slot);
        }

        return entropy - 1;
    }

    public void ForEachWave(uint[] waveFunction, Func<int, bool> action) {
        uint order = 0U;

        // Iterate slotwise
        foreach (uint s in waveFunction) {
            uint slot = s;
            // Iterate bitwise
            for (uint i = 0U; i < sizeof(uint) * 8; ++i) {
                // End if slot is empty
                if (slot == 0U)
                    break;

                // If current bit is 1, call the function
                if ((slot & 1U) == 1U) {
                    // Call the callback
                    if (action((int)(order + i)))
                        break;
                }

                // Shift the slot
                slot >>= 1;
            }

            // Increment order
            order += sizeof(uint) * 8;
        }
    }

    /**
     * Iteration function. Apply the action for each wave in the waveFunction
     */
    public void ForEachWave(uint[] waveFunction, Action<int> action) {
        this.ForEachWave(waveFunction, (order) => {
            action(order);
            return false;
        });
    }

    /**
     * Decode the wave function to get the actual waves
     */
    public Wave[] GetWaves(uint[] waveFunction) {
        // Create the list for waves
        List<Wave> wavesInFunction = new List<Wave>();

        Console.WriteLine("A: " + waveFunction[0]);
        // For each wave in wave function
        this.ForEachWave(waveFunction, (order) => {
            Console.WriteLine("B: " + order);
            wavesInFunction.Add(this.orderWave[order]);
        });

        // Convert to array
        return wavesInFunction.ToArray();
    }
}
