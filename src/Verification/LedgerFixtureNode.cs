using Assets.Source.Flow;
using LightJson;
using UnityEngine;

namespace KingdomGlobalLedger.Verification
{
    internal sealed class LedgerFixtureNode : FlowElement
    {
        internal string Kind = "V2Fixture";
        public FlowDiagram InnerFlow { get; set; }
        public override string ElementType => Kind;
        public override string Label => "Global Ledger fixture";
        public override string IconName => null;
        public override ElementCategory Category => ElementCategory.Utility;
        internal LedgerFixtureNode()
        {
            Size = new Vector2Int(128, 128);
            Inputs.Add(new FlowInput(this, 0));
            Outputs.Add(new FlowOutput(this, 0));
            Outputs.Add(new FlowOutput(this, 1));
        }
        public override void Update(float delta) { }
        protected override void DataToJson(JsonObject data) { }
        protected override void DataFromJson(JsonObject data, FlowDiagram parent) { }
    }
}
