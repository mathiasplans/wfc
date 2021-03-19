using System;

public class WFCService {
    public static void Main() {
        // Imitator2D imit = new Imitator2D("wfc/wang-2c.png", 32);
        // imit.Imitate(32, 32);
        // imit.Save("MyCreation.png");

        Demo d = new Demo(40, 40);
        d.Save("MyCreation.png");
    }
}