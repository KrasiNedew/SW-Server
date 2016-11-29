namespace ServerUtils
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;

    public class BlockingCircularQueue<T, K>
    {
        private readonly BlockingCollection<KeyValuePair<T, K>> queue;

        private readonly int size;

        public BlockingCircularQueue(int size)
        {
            this.queue = new BlockingCollection<KeyValuePair<T, K>>(size + 1);
            this.size = size;
        }

        public KeyValuePair<T, K> Dequeue()
        {
            return this.queue.Take();
        }

        //public void Enqueue(KeyValuePair<T, K> item)
        //{
        //    bool added = false;
        //    if (this.queue.Count < this.size)
        //    {
        //        added = this.queue.TryAdd(item);
        //    }

        //    if(!added)
        //    {
        //        out
        //        KeyValuePair<T, K> item;
        //        this.queue.TryTake()
        //    }
        //}
    }
}