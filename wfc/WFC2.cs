using System.Collections;
using System.Collections.Generic;
using System;
using System.Diagnostics;

public class WFC2 {
    private uint dimx, dimy;
    private uint[,][] grid;
    private uint[,] entropy;

    private List<Wave> waves;

    private WaveFunction core;

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
     * Add multiple waves at once
     */
    public void AddWaves(Wave[] waves) {
        this.waves.AddRange(waves);
    }

    /**
     * Encode the wave function
     */
    public void Encode() {
        // 2D grid has 4 directions: North, South, East, West
        this.core = new WaveFunction(this.waves.ToArray(), 4);
        Wave[] cw = this.core.GetSymmetryFaults();
        if (cw != null)
            throw new Exception($"Given waves' adjacency rules are not symmetrical: {cw[0].name} and {cw[1].name}");
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
        uint[] constraint = this.core.wencoder.GetPossibilitySpace(waves);
        uint entropy = this.core.wencoder.GetEntropy(constraint);
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
                    waveFunction = this.core.wencoder.GetPossibilitySpace();
                    this.grid[x, y] = waveFunction;
                    this.entropy[x, y] = this.core.wencoder.GetEntropy(this.grid[x, y]);
                }

                // else {
                //     Console.WriteLine($"There is a constraint on the grid: ({x}, {y}) @{this.entropy[x, y]}");
                // }
            }
        }
    }

    public Wave[] GetWaves(uint x, uint y) {
        uint[] place = this.grid[x, y];

        if (place == null)
            throw new Exception("The grid has not been initialized. Add a constraint or use the FillGrid function.");

        return this.core.wencoder.GetWaves(place);
    }

    public Wave GetWave(uint x, uint y) {
        Wave[] waves = this.GetWaves(x, y);

        if (!(waves.Length > 0))
            throw new Exception("This place has negative entropy: " + x + ", " + y);

        return waves[0];
    }

    struct Tile : IComparable {
        public uint x;
        public uint y;

        public int CompareTo(Object other) {
            return this.GetHashCode() - other.GetHashCode();
        }

        public Tile(uint x, uint y) {
            this.x = x;
            this.y = y;
        }

        public void Set(uint x, uint y) {
            this.x = x;
            this.y = y;
        }

        public override bool Equals(object obj) {
            Tile o = (Tile) obj;
            return o.x == this.x && o.y == this.y;
        }

        public override int GetHashCode() {
            int hashCode = 0;
            hashCode = (hashCode * 397) ^ this.x.GetHashCode();
            hashCode = (hashCode * 397) ^ this.y.GetHashCode();
            return hashCode;
        }
    }

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

    private uint Propagate(Tile lowestEntropyTile, ClassSet<Tile> entropyClasses) {
        ClassSet<Tile> propagation = new ClassSet<Tile>();
        uint done = 0;

        // Now we need to propagate the change
        propagation.Add(0, lowestEntropyTile);

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

        // Run until the state has been completly propagated.
        // In other words, propagate until there is nothing more
        // to propagate. AKA until propagation stack has no more
        // elements.
        // TODO: This loop is probably the most expensive piece
        // of code in this project. Optimize! (make less brancy)
        while (propagation.Count > 0) {

            // Get the propagator
            uint propagatorClass = propagation.GetMinClass();
            pt = propagation.RandomFromClass(propagatorClass);
            propagation.Remove(propagatorClass, pt);

            propagator = this.grid[pt.x, pt.y];

            validDirection[WFC2.NORTH] = false;
            validDirection[WFC2.EAST] = false;
            validDirection[WFC2.SOUTH] = false;
            validDirection[WFC2.WEST] = false;

            // Propagate to north
            if (pt.y > 0) {
                propagationDirections[WFC2.NORTH].Set(pt.x, pt.y - 1);
                validDirection[WFC2.NORTH] = true;
            }

            // Propagate to east
            if (pt.x < this.dimx - 1) {
                propagationDirections[WFC2.EAST].Set(pt.x + 1, pt.y);
                validDirection[WFC2.EAST] = true;
            }

            // Propagate to south
            if (pt.y < this.dimy - 1) {
                propagationDirections[WFC2.SOUTH].Set(pt.x, pt.y + 1);
                validDirection[WFC2.SOUTH] = true;
            }

            // Propagate to west
            if (pt.x > 0) {
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
#if(DEBUG)
                if (newEntropy > ~0U - 100)
                    throw new Exception($"New entropy is negative ({newEntropy}, was {propageeEntropy}): ({pt.x}, {pt.y}) propagated to ({dt.x}, {dt.y}), propagator {Convert.ToString(propagator[0], 2)}");
#endif
                // The wave function has changed
                if (propageeEntropy != newEntropy) {
                    // Change the entropy class
                    entropyClasses.ChangeClass(propageeEntropy, newEntropy, dt);

                    // Update done
                    if (newEntropy == 0)
                        done += 1;

                    // Update the entropy
                    this.entropy[dt.x, dt.y] = newEntropy;

                    // This will also propagate
                    Tile nt = new Tile(dt.x, dt.y);
                    propagation.Add(newEntropy, nt);
                    propagation.Remove(propageeEntropy, nt);
                }
            }
        }

        return done;
    }

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
        // SortedSet<Tile>[] entropyClasses = new SortedSet<Tile>[this.waves.Count];
        // for (uint i = 0; i < this.waves.Count; ++i) {
        //     entropyClasses[i] = new SortedSet<Tile>();
        // }
        ClassSet<Tile> entropyClasses = new ClassSet<Tile>();

        // Console.WriteLine("Start writing");
        // Fill the classes
        this.ForEachTile((x, y) => {
            uint entropy = this.entropy[x, y];
            entropyClasses.Add(entropy, new Tile(x, y));
            // Console.WriteLine($"Writing {x} {y} @{entropy}");
            // Console.WriteLine($"Size is {entropyClasses.ClassSize(entropy)}");
        });

        if (this.dimx * this.dimy != entropyClasses.Count)
            throw new Exception($"Some tiles are missing from the entropy classes: expected {this.dimx * this.dimy}, is {entropyClasses.Count}");
        // Console.WriteLine($"{this.dimx} {this.dimy}");
        // Console.WriteLine("Class set status: ");
        // entropyClasses.PrintCounts();

        Tile lowestEntropyTile = new Tile(0, 0);
        uint lowestEntropy = 0;
        ClassSet<Tile> propagation = new ClassSet<Tile>();

        // Execute until every tile has 0 entropy
        while (done < need) {
            // Get the tile with lowest entropy
            // The value of lowestEntropyTile should be non-default after
            // the loop. If it is not, then there is a severe bug
            // somewhere in this loop.
            for (uint i = 0; i < this.waves.Count; ++i) {
                if (entropyClasses.ClassSize(i) > 0) {
                    // Take an arbitrary element from the sent
                    lowestEntropyTile = entropyClasses.RandomFromClass(i);
                    lowestEntropy = i;

                    break;
                }
            }

            // Console.WriteLine($"{lowestEntropyTile.x} {lowestEntropyTile.y} @{lowestEntropy}");

            // If the tile has not been propagated yet
            uint did = this.Propagate(lowestEntropyTile, entropyClasses);
            if (did != 0) {
                done += did;
                continue;
            }

            // Get the coordinates
            uint x = lowestEntropyTile.x;
            uint y = lowestEntropyTile.y;

            // Collapse the wave on the tile
            this.core.Collapse(this.grid[x, y], this.entropy[x, y]);

            // Change the entropy class
            entropyClasses.ChangeClass(lowestEntropy, 0, lowestEntropyTile);
            done += 1;
            this.entropy[x, y] = 0;

            done += this.Propagate(lowestEntropyTile, entropyClasses);
        }
    }
}
