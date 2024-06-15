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
        private GameObject chalkboo;
        private Cell cell;
        public Vector3 CellPos => cell.CenterWorldPosition;
        public Quaternion PortalRotat => chalkboo.transform.rotation * Quaternion.Euler(0f,180f,0f);

        public CryingPortal Spawn(Cell tile)
        {
            room = tile.room;
            cell = tile;
            room.functions.AddFunction(this);
            chalkboo = transform.Find("Chalkbaord").gameObject;
            var quad = chalkboo.transform.Find("Quad").gameObject;
            Direction direction = tile.RandomUncoveredDirection(new System.Random());
            chalkboo.transform.parent = tile.TileTransform;
            chalkboo.transform.rotation = direction.ToRotation();
            tile.HardCoverWall(direction, true);
            transform.position = tile.room.ec.RealRoomMid(tile.room) + Vector3.up * 5f;

            MaterialModifier.ChangeHole(quad.GetComponent<MeshRenderer>(), mask, quad.GetComponent<MeshRenderer>().materials[1]);
            MaterialModifier.SetBase(quad.GetComponent<MeshRenderer>(), BasePlugin.bcppAssets.Get<RenderTexture>("MrPortalMan/ManCamRender"));
            return this;
        }
    }
}
