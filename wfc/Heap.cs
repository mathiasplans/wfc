using System.Collections;
using System.Collections.Generic;
using System;

public class Heap<T> {
    private List<T> heap;   
    private IComparer comparer;
    private Dictionary<T, int> indices;

    public int Count {get {return this.heap.Count;}}

    public Heap(IComparer comparer) {
        this.heap = new List<T>();
        this.comparer = comparer;
        this.indices = new Dictionary<T, int>();
    }

    private void Swap(int lhs, int rhs) {
        T temp = this.heap[lhs];
        this.heap[lhs] = this.heap[rhs];
        this.heap[rhs] = temp;

        // Update the indices in the dict
        this.indices[this.heap[lhs]] = lhs;
        this.indices[this.heap[rhs]] = rhs;
    }

    private static int Parent(int key) {
        return (key - 1) / 2;
    }

    private static int Left(int key) {
        return 2 * key + 1;
    }

    private static int Right(int key) {
        return 2 * key + 1;
    }

    public void Add(T item) {
        int i = this.Count;

        // Firstly, add the item to the end of the heap
        this.heap.Add(item);
        this.indices.Add(item, i);

        // Now bubble the item up until it is in the right place
        while (i != 0 && this.comparer.Compare(this.heap[i], this.heap[Parent(i)]) > 0) {
            this.Swap(i, Parent(i));
            i = Parent(i);
        }
    }

    private void RemoveLast() {
        int i = this.Count - 1;
        this.indices.Remove(this.heap[i]);
        this.heap.RemoveAt(i);
    }

    public T Peek() {
        return this.heap[0];
    }

    public T Pop() {
        if (this.Count == 0)
            return default(T);

        // Store the minimum value
        T min = this.heap[0];

        // Replace the tip
        this.heap[0] = this.heap[this.Count - 1];
        this.RemoveLast();

        // Re-heapify the heap
        Heapify(0);

        return min;
    }

    public void Heapify(int key) {
        int l = Left(key);
        int r = Right(key);

        int smallest = key;
        if (l < this.Count && this.comparer.Compare(this.heap[l], this.heap[smallest]) > 0)
            smallest = l;

        if (r < this.Count && this.comparer.Compare(this.heap[r], this.heap[smallest]) > 0)
            smallest = r;

        if (smallest != key) {
            this.Swap(key, smallest);
            this.Heapify(smallest);
        }
    }

    public void Decrease(T oldTile, T newTile) {
        int key = this.indices[oldTile];

        this.indices.Remove(oldTile);
        this.indices.Add(newTile, key);

        this.heap[key] = newTile;

        while (key != 0 && this.comparer.Compare(this.heap[key], this.heap[Parent(key)]) > 0) {
            this.Swap(key, Parent(key));
            key = Parent(key);
        }
    }

    public bool Contains(T item) {
        return this.indices.ContainsKey(item);
    }
}