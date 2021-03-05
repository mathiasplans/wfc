using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

public class Demo {
    Bitmap src;
    List<Color[]> sprites;
    List<Wave> waves;
    Dictionary<Wave, Color[]> waveSprite;
    int dim;
    int imgh, imgw;

    int newx, newy;

    WFC2 wfc;

    public Demo(int newx, int newy) {
        this.newx = newx;
        this.newy = newy;

        this.src = new Bitmap("wfc/greek.png");
        this.sprites = new List<Color[]>();
        this.dim = 32;
        this.imgh = this.src.Height;
        this.imgw = this.src.Width;
        this.waves = new List<Wave>();
        this.waveSprite = new Dictionary<Wave, Color[]>();

        for (int y = 0; y < this.imgh; y += this.dim) {
            for (int x = 0; x < this.imgw; x += this.dim) {
                // Load all the pixels
                Color[] sprite = new Color[this.dim * this.dim];
                for (int sy = 0; sy < this.dim; ++sy) {
                    for (int sx = 0; sx < this.dim; ++sx) {
                        int index = sx + this.dim * sy;
                        sprite[index] = this.src.GetPixel(x + sx, y + sy);
                    }
                }

                this.sprites.Add(sprite);
            }
        }

        // Define the waves
        // I:
        // Wall end north
        Wave eN = new Wave(4, "EndN");
        this.waves.Add(eN);
        this.waveSprite.Add(eN, this.sprites[0]);

        // II:
        // Wall corner north-west
        Wave cNW = new Wave(4, "CornerNW");
        this.waves.Add(cNW);
        this.waveSprite.Add(cNW, this.sprites[1]);

        // III:
        // Wall T south
        Wave tS = new Wave(4, "TS");
        this.waves.Add(tS);
        this.waveSprite.Add(tS, this.sprites[2]);

        // IV:
        // Wall corner north-east
        Wave cNE = new Wave(4, "CornerNE");
        this.waves.Add(cNE);
        this.waveSprite.Add(cNE, this.sprites[3]);

        // V:
        // Wall vertical
        Wave v = new Wave(4, "V");
        this.waves.Add(v);
        this.waveSprite.Add(v, this.sprites[4]);

        // VI:
        // Wall T west
        Wave tE = new Wave(4, "TE");
        this.waves.Add(tE);
        this.waveSprite.Add(tE, this.sprites[5]);

        // VII:
        // Wall cross
        Wave cross = new Wave(4, "Cross", 2);
        this.waves.Add(cross);
        this.waveSprite.Add(cross, this.sprites[6]);

        // VIII:
        // Wall T east
        Wave tW = new Wave(4, "TW");
        this.waves.Add(tW);
        this.waveSprite.Add(tW, this.sprites[7]);

        // IX:
        // Wall end south
        Wave eS = new Wave(4, "EndS");
        this.waves.Add(eS);
        this.waveSprite.Add(eS, this.sprites[8]);

        // X:
        // Wall corner south-west
        Wave cSW = new Wave(4, "CornerSW");
        this.waves.Add(cSW);
        this.waveSprite.Add(cSW, this.sprites[9]);

        // XI:
        // Wall T north
        Wave tN = new Wave(4, "TN");
        this.waves.Add(tN);
        this.waveSprite.Add(tN, this.sprites[10]);
        

        // XII:
        // Wall corner south-east
        Wave cSE = new Wave(4, "CornerSE", 8);
        this.waves.Add(cSE);
        this.waveSprite.Add(cSE, this.sprites[11]);

        // XIII:
        // Wall pillar
        Wave pillar = new Wave(4, "Pillar");
        this.waves.Add(pillar);
        this.waveSprite.Add(pillar, this.sprites[12]);

        // XIV:
        // Wall end west
        Wave eW = new Wave(4, "EndW");
        this.waves.Add(eW);
        this.waveSprite.Add(eW, this.sprites[13]);

        // XV:
        // Wall horisontal
        Wave h = new Wave(4, "H");
        this.waves.Add(h);
        this.waveSprite.Add(h, this.sprites[14]);

        // XVI:
        // Wall end east
        Wave eE = new Wave(4, "EndE");
        this.waves.Add(eE);
        this.waveSprite.Add(eE, this.sprites[15]);

        // Adjacensies
        Wave[] connectedNorth = new Wave[] {eN, cNW, tS, cNE, v, tE, cross, tW};
        Wave[] connectedEast = new Wave[] {tS, cNE, cross, tW, tN, cSE, h, eE};
        Wave[] connectedSouth = new Wave[] {v, tE, cross, tW, eS, cSW, tN, cSE};
        Wave[] connectedWest = new Wave[] {cNW, tS, tE, cross, cSW, tN, eW, h};

        Wave[] blankNorth = new Wave[] {eS, cSW, tN, cSE, pillar, eW, h, eE};
        Wave[] blankEast = new Wave[] {eN, cNW, v, tE, eS, cSW, pillar, eW};
        Wave[] blankSouth = new Wave[] {eN, cNW, tS, cNE, pillar, eW, h, eE};
        Wave[] blankWest = new Wave[] {eN, cNE, v, tW, eS, cSE, pillar, eE};

        eN.AddConstraints(WFC2.NORTH, blankNorth);
        eN.AddConstraints(WFC2.EAST, blankEast);
        eN.AddConstraints(WFC2.SOUTH, connectedSouth);
        eN.AddConstraints(WFC2.WEST, blankWest);

        cNW.AddConstraints(WFC2.NORTH, blankNorth);
        cNW.AddConstraints(WFC2.EAST, connectedEast);
        cNW.AddConstraints(WFC2.SOUTH, connectedSouth);
        cNW.AddConstraints(WFC2.WEST, blankWest);

        tS.AddConstraints(WFC2.NORTH, blankNorth);
        tS.AddConstraints(WFC2.EAST, connectedEast);
        tS.AddConstraints(WFC2.SOUTH, connectedSouth);
        tS.AddConstraints(WFC2.WEST, connectedWest);

        cNE.AddConstraints(WFC2.NORTH, blankNorth);
        cNE.AddConstraints(WFC2.EAST, blankEast);
        cNE.AddConstraints(WFC2.SOUTH, connectedSouth);
        cNE.AddConstraints(WFC2.WEST, connectedWest);

        v.AddConstraints(WFC2.NORTH, connectedNorth);
        v.AddConstraints(WFC2.EAST, blankEast);
        v.AddConstraints(WFC2.SOUTH, connectedSouth);
        v.AddConstraints(WFC2.WEST, blankWest);

        tE.AddConstraints(WFC2.NORTH, connectedNorth);
        tE.AddConstraints(WFC2.EAST, connectedEast);
        tE.AddConstraints(WFC2.SOUTH, connectedSouth);
        tE.AddConstraints(WFC2.WEST, blankWest);

        cross.AddConstraints(WFC2.NORTH, connectedNorth);
        cross.AddConstraints(WFC2.EAST, connectedEast);
        cross.AddConstraints(WFC2.SOUTH, connectedSouth);
        cross.AddConstraints(WFC2.WEST, connectedWest);
        
        tW.AddConstraints(WFC2.NORTH, connectedNorth);
        tW.AddConstraints(WFC2.EAST, blankEast);
        tW.AddConstraints(WFC2.SOUTH, connectedSouth);
        tW.AddConstraints(WFC2.WEST, connectedWest);

        eS.AddConstraints(WFC2.NORTH, connectedNorth);
        eS.AddConstraints(WFC2.EAST, blankEast);
        eS.AddConstraints(WFC2.SOUTH, blankSouth);
        eS.AddConstraints(WFC2.WEST, blankWest);

        cSW.AddConstraints(WFC2.NORTH, connectedNorth);
        cSW.AddConstraints(WFC2.EAST, connectedEast);
        cSW.AddConstraints(WFC2.SOUTH, blankSouth);
        cSW.AddConstraints(WFC2.WEST, blankWest);

        tN.AddConstraints(WFC2.NORTH, connectedNorth);
        tN.AddConstraints(WFC2.EAST, connectedEast);
        tN.AddConstraints(WFC2.SOUTH, blankSouth);
        tN.AddConstraints(WFC2.WEST, connectedWest);

        cSE.AddConstraints(WFC2.NORTH, connectedNorth);
        cSE.AddConstraints(WFC2.EAST, blankEast);
        cSE.AddConstraints(WFC2.SOUTH, blankSouth);
        cSE.AddConstraints(WFC2.WEST, connectedWest);

        pillar.AddConstraints(WFC2.NORTH, blankNorth);
        pillar.AddConstraints(WFC2.EAST, blankEast);
        pillar.AddConstraints(WFC2.SOUTH, blankSouth);
        pillar.AddConstraints(WFC2.WEST, blankWest);

        eW.AddConstraints(WFC2.NORTH, blankNorth);
        eW.AddConstraints(WFC2.EAST, connectedEast);
        eW.AddConstraints(WFC2.SOUTH, blankSouth);
        eW.AddConstraints(WFC2.WEST, blankWest);

        h.AddConstraints(WFC2.NORTH, blankNorth);
        h.AddConstraints(WFC2.EAST, connectedEast);
        h.AddConstraints(WFC2.SOUTH, blankSouth);
        h.AddConstraints(WFC2.WEST, connectedWest);

        eE.AddConstraints(WFC2.NORTH, blankNorth);
        eE.AddConstraints(WFC2.EAST, blankEast);
        eE.AddConstraints(WFC2.SOUTH, blankSouth);
        eE.AddConstraints(WFC2.WEST, connectedWest);

        this.wfc = new WFC2((uint) this.newx, (uint) this.newy);
        this.wfc.AddWaves(waves.ToArray());
        this.wfc.Encode();
        this.wfc.FillGrid();
        this.wfc.Collapse();
    }

    public void Save(string filename) {
        int width = this.dim * (int) this.newx;
        int height = this.dim * (int) this.newy;

        Bitmap bmp = new Bitmap(width, height);

        for (int y = 0; y < this.newy; ++y) {
            for (int x = 0; x < this.newx; ++x) {
                // Get the hash
                Wave w = this.wfc.GetWave((uint) x, (uint) y);

                // Get the image
                Color[] sprite = this.waveSprite[w];

                // Now copy it onto the output
                for (int sy = 0; sy < this.dim; ++sy) {
                    for (int sx = 0; sx < this.dim; ++sx) {
                        int index = sx + sy * this.dim;

                        Color pixel = sprite[index];
                        bmp.SetPixel(x * this.dim + sx, y * this.dim + sy, pixel);
                    }
                }
            }
        }

        // Save the bitmap into a file
        bmp.Save(filename, ImageFormat.Png);
    }
}