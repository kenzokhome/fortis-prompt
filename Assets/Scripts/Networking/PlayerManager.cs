using Core.Player;
using Fortis.LAN;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class PlayerManager : IEnumerable<Player>
{
    public abstract IEnumerator<Player> GetEnumerator();
    public abstract int Count { get; }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
