using System.Collections;
using System.Collections.Generic;
using System;

public class WaveFunctionEncoder {
    private int size;
    private uint[] encoded;
    private uint[,][] constraints;
    private int encodeSize;

    private Dictionary<Wave, int> waveOrder;
    private Dictionary<int, Wave> orderWave;

    private void Initialize(Wave[] waves, uint adjacencies) {
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

        // Now create the rules (constraints guide)
        this.constraints = new uint[adjacencies, this.size][];

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
     * Constructor for naive wave function collapse. The context of adjacency (e.g direction)
     * does not matter. This is useful when running the wave function collapse algorithm on a
     * graph.
     */
    public WaveFunctionEncoder(Wave[] waves) {
        this.Initialize(waves, 1);
    }

    /**
     * Constructor for wave function collapse with contextual adjacency. The adjacency context matters -
     * rules in south and north are different, for example. This is useful when running the wave function
     * collapse algorithm on a grid.
     */
    public WaveFunctionEncoder(Wave[] waves, uint adjacencies) {
        this.Initialize(waves, adjacencies);
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
     * Sets a specific bit and resets everything else
     */
    private void SelectWave(uint[] waveFunction, int order) {
        for (uint i = 0; i < this.encodeSize; ++i) {
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

    private void ForEachWave(uint[] waveFunction, Func<int, bool> action) {
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
                    if (action((int)(order + i)))
                        break;
                }

                // Shift the slot
                slot >>= 1;
            }

            // Increment order
            order += 8U;
        }
    }

    /**
     * Iteration function. Apply the action for each wave in the waveFunction
     */
    private void ForEachWave(uint[] waveFunction, Action<int> action) {
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
        Console.WriteLine("b" + Convert.ToString(observer[0], 2));
        Console.WriteLine("b" + Convert.ToString(collapsable[0], 2));
        // For each wave in the observer
        this.ForEachWave(observer, (order) => {
            // And the constraint with collapsable
            this.AndInPlace(collapsable, this.constraints[adjacency, order]);
        });

        // Get the new entropy
        return this.GetEntropy(collapsable);
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
        this.ForEachWave(collapsable, (order) => {
            ++i;
            rankOrder = order;
            return i == rank;
        });

        // Set the bit at rankOrder and reset all the other bits
        this.SelectWave(collapsable, rankOrder);
    }
}
