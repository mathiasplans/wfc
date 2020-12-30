using System.Collections;
using System.Collections.Generic;
using System;

public class WFC2 {
    private uint dimx, dimy;
    private uint[,][] grid;
    private uint[,] entropy;

    private List<Wave> waves;

    private WaveFunctionEncoder core;

    /**
     * Wave function collapse for 2D grid
     */
    public WFC2(uint x, uint y) {
        this.dimx = x;
        this.dimy = y;

        this.grid = new uint[x, y][];
        this.entropy = new uint[x, y];
        this.waves = new List<Wave>();
    }

    /**
     * Add a possible state to the wave function
     * This has to be done before calling the Encode function
     */
    public void AddWave(Wave wave) {
        this.waves.Add(wave);
    }

    /**
     * Encode the wave function
     */
    public void Encode() {
        // 2D grid has 4 directions: North, South, East, West
        this.core = new WaveFunctionEncoder(this.waves.ToArray(), 4);
    }

    /**
     * Add a special constraint to the grid.
     * This can only be used after calling the Encode function.
     */
    public void AddConstraint(uint x, uint y, Wave wave) {
        Wave[] monowave = new Wave[1];
        monowave[0] = wave;
        AddConstraint(x, y, monowave);
    }

    /**
     * Add a special constraint to the grid.
     * This can only be used after calling the Encode function.
     */
    public void AddConstraint(uint x, uint y, Wave[] waves) {
        uint[] constraint = this.core.GetPossibilitySpace(waves);
        uint entropy = this.core.GetEntropy(constraint);
        this.grid[x, y] = constraint;
        this.entropy[x, y] = entropy;
    }

    /**
     * Fill the grid with maximum entropy wave functions.
     * This has to be called when custom constraints have been added.
     */
    public void FillGrid() {
        uint[] waveFunction;
        for (uint x = 0; x < this.dimx; ++x) {
            for (uint y = 0; y < this.dimy; ++y) {
                if (this.grid[x, y] == null) {
                    waveFunction = this.core.GetPossibilitySpace();
                    this.grid[x, y] = waveFunction;
                    this.entropy[x, y] = this.core.GetEntropy(waveFunction);
                }
            }
        }
    }

    struct Tile {
        public uint x;
        public uint y;

        public Tile(uint x, uint y) {
            this.x = x;
            this.y = y;
        }

        public void Set(uint x, uint y) {
            this.x = x;
            this.y = y;
        }
    };

    private void ForEachTile(Action<uint, uint> action) {
        for (uint x = 0; x < this.dimx; ++x) {
            for (uint y = 0; y < this.dimy; ++y) {
                action(x, y);
            }
        }
    }

    public static readonly uint NORTH = 0;
    public static readonly uint EAST = 1;
    public static readonly uint SOUTH = 2;
    public static readonly uint WEST = 3;

    /**
     * Collapse the wave function
     */
    public void Collapse() {
        uint done = 0;
        bool[,] finished = new bool[this.dimx, this.dimy];
        uint need = this.dimx * this.dimy;

        // Initialize the finished array
        this.ForEachTile((x, y) => {
            if (this.entropy[x, y] == 0) {
                finished[x, y] = true;
                done += 1;
            }
        });

        // Split the tiles into entropy classes
        SortedSet<Tile>[] entropyClasses = new SortedSet<Tile>[this.waves.Count];
        for (uint i = 0; i < this.waves.Count; ++i) {
            entropyClasses[i] = new SortedSet<Tile>();
        }

        // Fill the classes
        this.ForEachTile((x, y) => {
            uint entropy  = this.entropy[x, y];
            entropyClasses[entropy].Add(new Tile(x, y));
        });

        Tile lowestEntropyTile = new Tile(0, 0);
        uint lowestEntropy = 0;
        Stack<Tile> propagation = new Stack<Tile>();

        // Execute until every tile has 0 entropy
        while (done < need) {
            // Get the tile with lowest entropy
            // The value of lowestEntropyTile should be non-default after
            // the loop. If it is not, then there is a severe bug
            // somewhere in this loop.
            for (uint i = 1; i < this.waves.Count; ++i) {
                if (entropyClasses[i].Count > 0) {
                    // Take an arbitrary element from the sent
                    lowestEntropyTile = entropyClasses[i].Min;
                    lowestEntropy = i;
                }
            }

            // Get the coordinates
            uint x = lowestEntropyTile.x;
            uint y = lowestEntropyTile.y;

            // Collapse the wave on the tile
            this.core.Collapse(this.grid[x, y], this.entropy[x, y]);

            // Change the entropy class
            entropyClasses[lowestEntropy].Remove(lowestEntropyTile);
            entropyClasses[0].Add(lowestEntropyTile); // TODO: do we need to do that?
            this.entropy[x, y] = 0;

            // Now we need to propagate the change
            propagation.Push(lowestEntropyTile);

            // Propagation tile
            Tile pt;

            // Propagator and propagee wave functions
            uint[] propagator, propagee;

            // Old and new entropy
            uint propageeEntropy;
            uint newEntropy;

            // Propagation directions and can we go in this direction
            Tile[] propagationDirections = new Tile[4];
            bool[] validDirection = new bool[4];

            // Run until the state has been completly been propagated.
            // In other words, propagate until there is nothing more
            // to propagate. AKA until propagation stack has no more
            // elements.
            // TODO: This loop is propably the most expensive piece
            // of code in this project. Optimize! (make less brancy)
            while (propagation.Count > 0) {
                // Get the propagator
                pt = propagation.Pop();
                propagator = this.grid[pt.x, pt.y];

                validDirection[WFC2.NORTH] = false;
                validDirection[WFC2.EAST] = false;
                validDirection[WFC2.SOUTH] = false;
                validDirection[WFC2.WEST] = false;

                // Propagate to north
                if (y > 0) {
                    propagationDirections[WFC2.NORTH].Set(pt.x, pt.y - 1);
                    validDirection[WFC2.NORTH] = true;
                }

                // Propagate to east
                if (x < this.dimx) {
                    propagationDirections[WFC2.EAST].Set(pt.x + 1, pt.y);
                    validDirection[WFC2.EAST] = true;
                }

                // Propagate to south
                if (y < this.dimy) {
                    propagationDirections[WFC2.SOUTH].Set(pt.x, pt.y + 1);
                    validDirection[WFC2.SOUTH] = true;
                }

                // Propagate to west
                if (x > 0) {
                    propagationDirections[WFC2.WEST].Set(pt.x - 1, pt.y);
                    validDirection[WFC2.WEST] = true;
                }

                // Propagate in each direction
                for (uint i = 0; i < 4; ++i) {
                    if (!validDirection[i])
                        continue;

                    // The tile where we are going to propagate now
                    Tile dt = propagationDirections[i];

                    // Get the propagee
                    propagee = this.grid[dt.x, dt.y];

                    // Old and new entropy (after propagating the state)
                    propageeEntropy = this.entropy[dt.x, dt.y];
                    newEntropy = this.core.Collapse(propagator, propagee, i);

                    // The wave function has changed
                    // TODO: Make so that the tile with the lowest entropy is pushed last
                    if (propageeEntropy != newEntropy) {
                        // Change the entropy class
                        entropyClasses[propageeEntropy].Remove(dt);
                        entropyClasses[newEntropy].Add(dt);

                        // Update the entropy
                        this.entropy[dt.x, dt.y] = newEntropy;

                        // This will also propagate
                        propagation.Push(new Tile(dt.x, dt.y));
                    }
                }
            }
        }
    }
}
