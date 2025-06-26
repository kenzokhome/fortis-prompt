using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Fortis.ObjectPool
{
    public class ObjectPoolContainer<T>
    {
        private List<ObjectPoolItem<T>> itemsList;
        private Dictionary<T, ObjectPoolItem<T>> usedItemlookup;
        private Func<T> factoryFunc;
        private int lastIndex = 0;
        private UnityEvent<T> warmEvent;

        public List<ObjectPoolItem<T>> ListOfObjects
        {
            get { return itemsList; }
        }

        public ObjectPoolContainer(Func<T> factoryFunc, int initialSize, UnityEvent<T> warmEvent)
        {
            this.factoryFunc = factoryFunc;
            this.warmEvent = warmEvent;
            itemsList = new List<ObjectPoolItem<T>>(initialSize);
            usedItemlookup = new Dictionary<T, ObjectPoolItem<T>>(initialSize);

            Warm(initialSize);
        }

        private void Warm(int capacity)
        {
            for (int i = 0; i < capacity; i++)
            {
                CreateItem();
            }
        }

        private ObjectPoolItem<T> CreateItem()
        {
            var item = new ObjectPoolItem<T>();
            item.Item = factoryFunc();
            itemsList.Add(item);
            if (warmEvent != null)
                warmEvent.Invoke(item.Item);
            return item;
        }

        public T GetItem()
        {
            ObjectPoolItem<T> item = null;
            for (int i = 0; i < itemsList.Count; i++)
            {
                lastIndex++;
                if (lastIndex > itemsList.Count - 1) lastIndex = 0;

                if (itemsList[lastIndex].Used)
                {
                    continue;
                }
                else
                {
                    item = itemsList[lastIndex];
                    break;
                }
            }

            if (item == null)
            {
                item = CreateItem();
            }

            item.Consume();
            usedItemlookup.Add(item.Item, item);
            return item.Item;
        }

        public void ReleaseItem(object item)
        {
            ReleaseItem((T)item);
        }

        public void ReleaseItem(T item)
        {
            if (usedItemlookup.ContainsKey(item))
            {
                var container = usedItemlookup[item];
                container.Release();
                usedItemlookup.Remove(item);
            }
            else
            {
                Debug.LogWarning("This object pool does not contain the item provided: " + item);
            }
        }

        public int Count
        {
            get { return itemsList.Count; }
        }

        public int CountUsedItems
        {
            get { return usedItemlookup.Count; }
        }
    }
}