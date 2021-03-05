using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

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
            hash ^= hash >> 12;
        }

        return (int) hash;
    }

    private int NeighborHash(int spriteHash, int[] nHash) {
        int newHash = 0x9876ABC;

        // Has to incorporate spriteHash, as well as
        // all the neighbours' hashes so that the
        // direction is preserved.
        newHash ^= spriteHash;
        newHash ^= (nHash[0] << 8) | (nHash[0] >> -8);
        newHash ^= (nHash[1] << 12) | (nHash[1] >> -12);
        newHash ^= (nHash[2] << 16) | (nHash[2] >> -16);
        newHash ^= (nHash[3] << 24) | (nHash[3] >> -24);

        return newHash;
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

        Dictionary<int, int> hashCount = new Dictionary<int, int>();
        int hashcount = 0;

        // Iterate all the cells
        for (int y = 0; y < this.imgh; y += this.dim) {
            for (int x = 0; x < this.imgw; x += this.dim) {
                // I: load all the pixels
                Color[] sprite = new Color[this.dim * this.dim];
                for (int sy = 0; sy < this.dim; ++sy) {
                    for (int sx = 0; sx < this.dim; ++sx) {
                        int index = sx + this.dim * sy;
                        sprite[index] = this.imitee.GetPixel(x + sx, y + sy);
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

        int[,] neighHashMap = new int[this.imgw / this.dim, this.imgh / this.dim];
        int[] nHashes = new int[4];
        Dictionary<int, int[]> neighbours = new Dictionary<int, int[]>();
        Dictionary<int, int> originals = new Dictionary<int, int>();
        Dictionary<int, HashSet<int>> news = new Dictionary<int, HashSet<int>>();
        for (int y = 1; y < this.imgh / this.dim - 1; ++y) {
            for (int x = 1; x < this.imgw / this.dim - 1; ++x) {
                // Get the hash of the cell
                int hash = this.hashMap[x, y];

                // Get the neighbouring hashes
                nHashes[0] = 0;
                nHashes[1] = 0;
                nHashes[2] = 0;
                nHashes[3] = 0;

                // Check the north
                nHashes[0] = this.hashMap[x, y - 1];

                // Check the east
                nHashes[1] = this.hashMap[x + 1, y];

                // Check the south
                nHashes[2] = this.hashMap[x, y + 1];

                // Check the west
                nHashes[3] = this.hashMap[x - 1, y];

                // Get the complete hash
                int completeHash = this.NeighborHash(hash, nHashes);
                neighHashMap[x, y] = completeHash;
                
                // Count the new hashes
                if (!hashCount.ContainsKey(completeHash)) {
                    hashCount.Add(completeHash, 1);
                    originals.Add(completeHash, hash);

                    // Add to the mapping from old to new
                    if (!news.ContainsKey(hash))
                        news.Add(hash, new HashSet<int>());

                    news[hash].Add(completeHash);
                }

                else 
                    hashCount[completeHash] += 1;
            }
        }

        // Detect the neighbours
        for (int y = 2; y < this.imgh / this.dim - 2; ++y) {
            for (int x = 2; x < this.imgw / this.dim - 2; ++x) {
                // Get the hash of the cell
                int hash = neighHashMap[x, y];

                if (neighbours.ContainsKey(hash))
                    continue;

                // Get the neighbouring hashes
                nHashes[0] = 0;
                nHashes[1] = 0;
                nHashes[2] = 0;
                nHashes[3] = 0;

                // Check the north
                nHashes[0] = neighHashMap[x, y - 1];

                // Check the east
                nHashes[1] = neighHashMap[x + 1, y];

                // Check the south
                nHashes[2] = neighHashMap[x, y + 1];

                // Check the west
                nHashes[3] = neighHashMap[x - 1, y];

                neighbours.Add(hash, new int[] {nHashes[0], nHashes[1], nHashes[2], nHashes[3]});
            }
        }

        // Weights
        int cellCount = this.imgh * this.imgw / (this.dim * this.dim);
        Dictionary<int, float> weights = new Dictionary<int, float>();
        Dictionary<int, Color[]> newHashToImage = new Dictionary<int, Color[]>();

        foreach (int hash in hashCount.Keys) {
            weights.Add(hash, ((float) hashCount[hash]) / ((float) cellCount));
        }

        // Remap the images from original hashes to new hashes
        foreach (int hash in hashCount.Keys) {
            newHashToImage.Add(hash, this.hashToImage[originals[hash]]);
        }

        this.hashToImage = newHashToImage;

        // We have all the info we need to start encoding it
        // Create all the necessary waves
        foreach (int hash in hashCount.Keys) {
            Wave wave = new Wave(4, hash.ToString(), weights[hash]);
            this.hashToWave.Add(hash, wave);
        }

        // Add the adjacencies
        foreach (int hash in neighbours.Keys) {
            Wave wave = this.hashToWave[hash];
            int[] adjs = neighbours[hash];

            // Adjacencies
            for (uint i = 0; i < 4; ++i) {
                if (adjs[i] == 0) {
                    wave.AddConstraints(i, new Wave[0]);
                    continue;
                }

                // Get all the alternatives
                int orig = originals[adjs[i]];
                HashSet<int> alts = news[orig];

                // As Waves
                List<Wave> nwaves = new List<Wave>();
                foreach (int ah in alts) {
                    nwaves.Add(hashToWave[ah]);
                }

                // Add the adjacencies
                wave.AddConstraints(i, nwaves.ToArray());
            }
        }

        // Now get the list of Waves
        List<Wave> waves = new List<Wave>();
        foreach (int hash in hashCount.Keys) {
            waves.Add(this.hashToWave[hash]);
        }

        Console.WriteLine("Total amount of waves: " + waves.Count);

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

        Bitmap bmp = new Bitmap(width, height);

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
                        bmp.SetPixel(x * this.dim + sx, y * this.dim + sy, pixel);
                    }
                }
            }
        }

        // Save the bitmap into a file
        bmp.Save(filename, ImageFormat.Png);
    }
}