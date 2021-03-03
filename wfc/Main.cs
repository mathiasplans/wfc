using System;

public class WFCService {
    public static void Main() {
        Imitator2D imit = new Imitator2D("wfc/jawbreaker_sample.png", 8);
        imit.Imitate(20, 40);
        imit.Save("MyCreation.png");
    }
}