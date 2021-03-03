using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System;
using System.Drawing;
using System.Drawing.Imaging;

public class Imitator2D {
    Bitmap imitee;
    int dim;
    int imgh, imgw;
    Dictionary<int, Color[]> hashToImage;
    Dictionary<int, HashSet<int>[]> adjacencies;
    Dictionary<int, Wave> hashToWave;
    int[,] hashMap;
    WFC2 wfc;

    uint newx, newy;

    private int SpriteHash(Color[] image) {
        uint hash = 0xABCDEF01;

        for (uint i = 0; i < image.Length; ++i) {
            hash ^= hash << 11 | (i * image[i].R * image[i].G * image[i].B) % 2048;
            hash ^= (8210921U - image[i].R) * i;
            hash ^= (98321U - image[i].B) * i;
            hash ^= (11127U + image[i].G) * i;
            hash ^= ((uint) image[i].R * image[i].G + i) * 29U;
            hash ^= ((uint) image[i].B) << (int)(12 + (i % 12));
        }

        return (int) hash;
    }

    public Imitator2D(string path, int dim) {
        this.imitee = new Bitmap(path);
        this.dim = dim;
        this.hashToImage = new Dictionary<int, Color[]>();
        this.adjacencies = new Dictionary<int, HashSet<int>[]>();
        this.hashToWave = new Dictionary<int, Wave>();

        this.imgh = this.imitee.Height;
        this.imgw = this.imitee.Width;

        this.hashMap = new int[this.imgw / dim, this.imgh / dim];
    }

    public void Imitate(uint newx, uint newy) {
        this.newx = newx;
        this.newy = newy;
        int hashcount = 0;
        // Iterate all the cells
        for (int y = 0; y < this.imgh; y += this.dim) {
            for (int x = 0; x < this.imgw; x += this.dim) {
                // I: load all the pixels
                Color[] sprite = new Color[this.dim * this.dim];
                for (int sy = 0; sy < this.dim; ++sy) {
                    for (int sx = 0; sx < this.dim; ++sx) {
                        sprite[sx + this.dim * sy] = this.imitee.GetPixel(x + sx, y + sy);
                    }
                }

                // II: Get the hash of the sprite
                int hash = this.SpriteHash(sprite);

                // III: Set it in the hashmap
                this.hashMap[x / this.dim, y / this.dim] = hash;

                // IV: If it's not in the hashToImage
                if (!this.hashToImage.ContainsKey(hash)) {
                    // Console.WriteLine("New hash #" + hashcount + " (" + x / this.dim + "," + y / this.dim  + ") " + sprite[0].R + ": " + hash);
                    hashcount++;
                    this.hashToImage.Add(hash, sprite);
                }
            }
        }

        // Now that we have loaded the image and split it into sprites,
        // we can start detecting the adjacency rules
        for (int y = 0; y < this.imgh / this.dim; ++y) {
            for (int x = 0; x < this.imgw / this.dim; ++x) {
                // Take the hash on this coordinate
                int hash = this.hashMap[x, y];

                // If the hash is encountered for the first time
                if (!this.adjacencies.ContainsKey(hash)) {
                    this.adjacencies.Add(hash, new HashSet<int>[4]);
                    this.adjacencies[hash][0] = new HashSet<int>();
                    this.adjacencies[hash][1] = new HashSet<int>();
                    this.adjacencies[hash][2] = new HashSet<int>();
                    this.adjacencies[hash][3] = new HashSet<int>();                
                }

                // Check the north
                if (y > 0) {
                    this.adjacencies[hash][0].Add(this.hashMap[x, y - 1]);
                }

                // Check the east
                if (x < this.imgw / this.dim - 1) {
                    this.adjacencies[hash][1].Add(this.hashMap[x + 1, y]);
                }

                // Check the south
                if (y < this.imgh / this.dim - 1) {
                    this.adjacencies[hash][2].Add(this.hashMap[x, y + 1]);
                }

                // Check the west
                if (x > 0) {
                    this.adjacencies[hash][3].Add(this.hashMap[x - 1, y]);
                }
            }
        }

        // We have all the info we need to start encoding it
        // Create all the necessary waves
        foreach (int hash in this.hashToImage.Keys) {
            Wave wave = new Wave(4, hash.ToString());
            this.hashToWave.Add(hash, wave);
        }

        // Add the adjacencies
        foreach (int hash in this.hashToImage.Keys) {
            Wave wave = this.hashToWave[hash];

            // Adjacencies
            for (uint i = 0; i < 4; ++i) {
                HashSet<int> adjs = this.adjacencies[hash][i];
                List<Wave> adjs_waves = new List<Wave>();

                // Take all the adjacencies from the set
                foreach (int adj_hash in adjs) {
                    adjs_waves.Add(this.hashToWave[adj_hash]);
                }

                // Add the adjacencies
                wave.AddConstraints(i, adjs_waves.ToArray());
            }
        }

        // Now get the list of Waves
        List<Wave> waves = new List<Wave>();
        foreach (int hash in this.hashToImage.Keys) {
            waves.Add(this.hashToWave[hash]);
        }

        Console.WriteLine(waves.Count);

        // Create WFC object
        this.wfc = new WFC2(newx, newy);
        this.wfc.AddWaves(waves.ToArray());
        this.wfc.Encode();
        this.wfc.FillGrid();
        this.wfc.Collapse();
    }

    public void Save(string filename) {
        int width = this.dim * (int) this.newx;
        int height = this.dim * (int) this.newy;

        uint[] image = new uint[width * height];
        var gchImage = GCHandle.Alloc(image, GCHandleType.Pinned);

        for (int y = 0; y < this.newy; ++y) {
            for (int x = 0; x < this.newx; ++x) {
                // Get the hash
                string hashstring = this.wfc.GetWave((uint) x, (uint) y).name;
                int hash = int.Parse(hashstring);

                // Get the image
                Color[] sprite = this.hashToImage[hash];

                // Now copy it onto the output
                for (int sy = 0; sy < this.dim; ++sy) {
                    for (int sx = 0; sx < this.dim; ++sx) {
                        int index = sx + sy * this.dim;

                        Color pixel = sprite[index];
                        uint red = pixel.R;
                        uint green = pixel.G;
                        uint blue = pixel.B;
                        image[index] = (red << 16) | (green << 8) | blue;
                    }
                }
            }
        }

        Bitmap newImage = new Bitmap(
            width, height, width * 3,
            PixelFormat.Format32bppPArgb,
            gchImage.AddrOfPinnedObject()
        );

        // Save the bitmap into a file
        newImage.Save(filename);

        // Unpin the memory region
        gchImage.Free();
    }
}