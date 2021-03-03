using System.Collections;
using System.Collections.Generic;
using System;

public class ClassSet<T> where T : IComparable {
    private Dictionary<uint, SortedSet<T>> setMap;
    public uint Count = 0;

    public ClassSet() {
        this.setMap = new Dictionary<uint, SortedSet<T>>();
    }

    public void Add(uint @class, T element) {
        if (!this.setMap.ContainsKey(@class))
            this.setMap.Add(@class, new SortedSet<T>());
    
        int c = this.setMap[@class].Count;
        this.setMap[@class].Add(element);

        // Increase the count if the number of elements changed
        this.Count += (uint) (this.setMap[@class].Count - c);
    }

    public void Remove(uint @class, T element) {
        if (!this.setMap.ContainsKey(@class) || !this.setMap[@class].Contains(element))
            return;

        this.setMap[@class].Remove(element);
        this.Count -= 1;
    }

    public void ChangeClass(uint oldClass, uint newClass, T element) {
        this.setMap[oldClass].Remove(element);
        this.Add(newClass, element);
    }

    public T RandomFromClass(uint @class) {
        T element = this.setMap[@class].Min;
        return element;
    }

    public uint GetMinClass() {
        uint minKey = ~0U;
        foreach (uint key in this.setMap.Keys) {
            if (minKey > key && this.setMap[key].Count > 0)
                minKey = key;
        }

        return minKey;
    }

    public uint GetMinClassNonZero() {
        uint minKey = ~0U;
        foreach (uint key in this.setMap.Keys) {
            if (minKey > key && this.setMap[key].Count > 0 && key != 0)
                minKey = key;
        }

        return minKey;
    }

    public int ClassSize(uint @class) {
        if (!this.setMap.ContainsKey(@class))
            return 0;

        else
            return this.setMap[@class].Count;
    }

    public void PrintCounts() {
        Console.WriteLine("Counts: ");
        foreach (uint key in this.setMap.Keys) {
            Console.WriteLine(key + ": " + this.setMap[key].Count);
        }
    }
}