using System.Numerics;
// using Unity.Burst;

public class WFCPopCount {
    static public int u32(uint word) {
        // For unity
        // return Unity.Burst.Intrinsics.X86.Popcnt.popcnt_u32(word);

        // For .NET 3.0+
        return BitOperations.PopCount(word);
    }
}
