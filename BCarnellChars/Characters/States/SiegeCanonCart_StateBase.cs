using System;
using System.Collections.Generic;
using System.Text;

namespace BCarnellChars.Characters.States
{
    public class SiegeCanonCart_StateBase : NpcState
    {
        protected SiegeCanonCart siegeCart;

        public SiegeCanonCart_StateBase(SiegeCanonCart siegecart)
            : base(siegecart)
        {
            siegeCart = siegecart;
        }
    }
}
