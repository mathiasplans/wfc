using System;

public class Root {
    static void Main() {
        Wave water = new Wave(4, "~");
        Wave coast = new Wave(4, "_");
        Wave land = new Wave(4, "-");
        Wave mountain = new Wave(4, "T");

        // Water constraints
        water.AddConstraints(WFC2.NORTH, new Wave[] {water});
        water.AddConstraints(WFC2.WEST, new Wave[] {water});
        water.AddConstraints(WFC2.SOUTH, new Wave[] {water, coast});
        water.AddConstraints(WFC2.EAST, new Wave[] {water, coast}); 

        // Coast constraints
        coast.AddConstraints(WFC2.NORTH, new Wave[] {water});
        coast.AddConstraints(WFC2.WEST, new Wave[] {water});
        coast.AddConstraints(WFC2.SOUTH, new Wave[] {land});
        coast.AddConstraints(WFC2.EAST, new Wave[] {land});

        // Land constraints
        land.AddConstraints(WFC2.NORTH, new Wave[] {coast, land, mountain});
        land.AddConstraints(WFC2.WEST, new Wave[] {coast, land, mountain});
        land.AddConstraints(WFC2.SOUTH, new Wave[] {land, mountain});
        land.AddConstraints(WFC2.EAST, new Wave[] {land, mountain});

        // Mountain constraint
        mountain.AddConstraints(WFC2.NORTH, new Wave[] {land, mountain});
        mountain.AddConstraints(WFC2.WEST, new Wave[] {land, mountain});
        mountain.AddConstraints(WFC2.SOUTH, new Wave[] {land, mountain});
        mountain.AddConstraints(WFC2.EAST, new Wave[] {land, mountain});

        // WFC2 kernel
        uint dimx = 20;
        uint dimy = 20;
        WFC2 waveFunctionCollapse = new WFC2(dimx, dimy);
        waveFunctionCollapse.AddWave(water);
        waveFunctionCollapse.AddWave(coast);
        waveFunctionCollapse.AddWave(land);
        waveFunctionCollapse.AddWave(mountain);

        waveFunctionCollapse.Encode();
        waveFunctionCollapse.AddConstraint(10, 9, new Wave[] {water});
        waveFunctionCollapse.FillGrid();

        // Collapse!
        waveFunctionCollapse.Collapse();
        
        // Visualize collapse

        for (uint x = 0; x < dimx; ++x) {
            for (uint y = 0; y < dimy; ++y) {
                Console.Write(waveFunctionCollapse.GetWave(x, y).name);
            }
            Console.Write("\n");
        }
    }
}