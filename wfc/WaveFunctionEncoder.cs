using System.Collections;
using System.Collections.Generic;
using System;

public class WaveFunctionEncoder {
    public readonly Wave[] waves;
    private int size;
    private uint[] encoded;
    private int encodeSize;
    private int encodeBytes;

    private Dictionary<Wave, int> waveOrder;
    private Dictionary<int, Wave> orderWave;

    private float[] weights;
    private float[,] weightLUT;

    private void CreateLUT() {
        // A method for getting the sum of weights
        void FillLUTColumn(int index, int until, int column,
                           float[,] LUT, uint LUTkey, float weight) {
            // Leaf
            if (index == until) {
                // Correct the LUTkey if the until is below 8
                LUTkey >>= 8 - until;
                LUT[column, LUTkey] = weight;
                return;
            }

            uint newKey = LUTkey >> 1;
            int newIndex = index + 1;

            FillLUTColumn(newIndex, until, column, LUT, newKey, weight);
            FillLUTColumn(newIndex, until, column, LUT, newKey | 128U, weight + this.weights[index]);
        }

        // Construct a lookup table for weigths
        this.weightLUT = new float[this.encodeBytes, 256];
        for (int i = 0; i < this.size / 8; ++i) {
            FillLUTColumn(8 * i, 8 * (i + 1), i, this.weightLUT, 0U, 0f);
        }

        int remainder = this.size % 8;
        FillLUTColumn(this.size - remainder, this.size, this.size / 8, this.weightLUT, 0U, 0f);
    }

    private void Initialize(Wave[] waves) {
        this.size = waves.Length;

        // Sort the waves by weight
        Array.Sort(waves, delegate(Wave w1, Wave w2) {
            return w2.Weight.CompareTo(w1.Weight);
        });

        // Get the weights
        this.weights = new float[this.size];
        float sumOfWeights = 0f;
        float weight;
        for (uint i = 0; i < this.size; ++i) {
            weight = waves[i].Weight;
            this.weights[i] = weight;
            sumOfWeights += weight;
        }

        // Normalize the weights
        for (uint i = 0; i < this.size; ++i) {
            this.weights[i] /= sumOfWeights;
        }

        // How many words have to be used
        uint spareMask = 0;
        this.encodeSize = this.size / 32;
        if (this.size % 32 != 0) {
            this.encodeSize += 1;
            spareMask = ~0U << (this.size % 32);
        }

        // How many bytes
        this.encodeBytes = this.size / 4;
        if (this.size % 4 != 0) {
            this.encodeBytes += 1;
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

        // LUT
        this.CreateLUT();
    }

    /**
     * Constructor for naive wave function collapse. The context of adjacency (e.g direction)
     * does not matter. This is useful when running the wave function collapse algorithm on a
     * graph.
     */
    public WaveFunctionEncoder(Wave[] waves) {
        this.waves = waves;
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
     * Get the full wave function
     */
    public uint[] GetFull() {
        return this.GetPossibilitySpace();
    }

    /**
     * Get the possibility space with some waves
     */
    public uint[] GetPossibilitySpace(ICollection<Wave> include) {
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
        // TODO: Add asserts
        for (uint i = 0; i < waveFunction.Length; ++i) {
            waveFunction[i] &= ander[i];
        }
    }

    public void OrInPlace(uint[] waveFunction, uint[] orer) {
        for (uint i = 0; i < waveFunction.Length; ++i) {
            waveFunction[i] |= orer[i];
        }
    }

    private float GetSumOfWeights(uint[] waveFunction) {
        // Get the sum of weigts
        float sow = 0f;
        int i = 0;
        foreach (uint segment in waveFunction) {
            uint s = segment;
            for (uint j = 0; j < sizeof(uint) && i < this.encodeBytes; ++j, ++i) {
                sow += this.weightLUT[i, s & 0xFFu];
                s >>= 8;
            }
        }

        return sow;
    }

    /**
     * Get the Shannone Entropy of the wave function.
     */
    public uint GetEntropy(uint[] waveFunction) {
        float sow = this.GetSumOfWeights(waveFunction);
#if(DEBUG)
        // The given wave function has no possible states (all zero)
        if (sow == 0f)
            throw new Exception("Requested the entropy of an empty wave function");
#endif

        float entropy = 0f;
        this.ForEachWave(waveFunction, (rank) => {
            float weight = this.weights[rank] / sow;

            // Fast approximation of x*log(x)
            entropy += weight * (1 - weight);
        });

        return (uint) (entropy * 10000f);
    }

    public void WeightedSelect(uint[] waveFunction, float r) {
        float sow = this.GetSumOfWeights(waveFunction);

        float random = sow * r;
        float cumulativeWeight = 0.0f;
        int seekRank = 0;
        this.ForEachWave(waveFunction, (rank) => {
            cumulativeWeight += this.weights[rank];
            if (random < cumulativeWeight) {
                seekRank = rank;
                return true;
            }

            return false;
        });

        float s = 0f;
        this.ForEachWave(waveFunction, (rank) => {
            s += this.weights[rank];
        });

        // Select the wave
        this.SelectWave(waveFunction, seekRank);
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

        // For each wave in wave function
        this.ForEachWave(waveFunction, (order) => {
            wavesInFunction.Add(this.orderWave[order]);
        });

        // Convert to array
        return wavesInFunction.ToArray();
    }

    public string FormatWave(uint[] waveFunction) {
        string o = "";
        foreach (uint segment in waveFunction) {
            o += Convert.ToString(segment, 2);
        }

        return o.PadLeft(this.size, '0');
    }
}
