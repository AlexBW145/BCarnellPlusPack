using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace BCarnellChars.GeneratorStuff
{
    public class CryingPortal : RoomFunction
    {
        public Material mask;

        public Transform Spawn(Cell tile)
        {
            room = tile.room;
            room.functions.AddFunction(this);
            Transform quad = transform.Find("Chalkbaord").Find("Quad").transform;
            Direction direction = tile.RandomUncoveredDirection(new System.Random());
            quad.transform.parent = tile.TileTransform;
            quad.transform.rotation = direction.ToRotation();
            tile.HardCoverWall(direction, true);
            transform.position = tile.room.ec.RealRoomMid(tile.room) + Vector3.up * 5f;

            MaterialModifier.ChangeHole(quad.GetComponent<MeshRenderer>(), mask, quad.GetComponent<MeshRenderer>().materials[1]);
            MaterialModifier.SetBase(quad.GetComponent<MeshRenderer>(), Resources.FindObjectsOfTypeAll<Texture2D>().ToList().Find(x => x.name == "StoreWJohnny_Blank"));
            return quad.transform;
        }
    }
}
