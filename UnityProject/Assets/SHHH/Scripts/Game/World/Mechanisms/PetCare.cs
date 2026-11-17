using System.Collections.Generic;
using SHHH.Core;
using TMPro;
using UnityEngine;

namespace SHHH.Game
{
    /// <summary>
    /// V12 pet needs in the world (owner design: hungry pets wake Mom - keep them fed). Rules state is the round's deterministic
    /// <c>PetHunger</c> (journaled clock + journaled meals), so every peer shows the same hunger.
    /// <list type="bullet">
    /// <item><b>Host:</b> a pet at its bowl eats the REAL food in the bowl (the bowl's finite volume, filled by tipping the food bag) and the
    /// grams eaten become a journaled <c>PetEat</c> (credit to whoever poured it in). A hungry pet whines / barks: a journaled pet-sound
    /// noise event that rises with hunger and comes more often - Mom hears it.</item>
    /// <item><b>Every peer:</b> a painted hunger badge over the pet (bowl icon + fill bar, pulsing when hungry, a heart when fed) and the
    /// bark / meow played from the replicated noise events (clients never invent noise).</item>
    /// </list>
    /// </summary>
    public sealed class PetCare : MonoBehaviour
    {
        public static readonly List<PetCare> All = new List<PetCare>();
        public PetKind Kind; public PetBody Body; public int ActorId; int _idx;
        RoomRuntime _room; PropBody _bowl; LiquidContainer _bowlFood;
        Transform _badge; Renderer _icon, _bar, _barBack; Material _hungry, _fed, _alert; TextMeshPro _label;
        float _nextWhine, _eatAcc, _eatFlushAt, _fedFlash, _lastEatTime = -99f; int _seenNoise = -1;
        public int Whines { get; private set; }
        float _lastReceived, _fillingUntil;
        public float EatenGrams { get; private set; }

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        public void Init(PetKind kind, PetBody body, RoomRuntime room)
        {
            Kind = kind; Body = body; _room = room; ActorId = body.ActorId; _idx = RoundController.PetIndex(ActorId);
            string bowlKind = kind == PetKind.Cat ? "pet_bowl_cat" : "pet_bowl_dog";
            foreach (var p in room.Def.Props) if (p.Kind == bowlKind) { _bowl = room.Prop(p.Id); break; }
            _bowlFood = _bowl != null ? Vessels.Of(_bowl) : null;
            // V12 (Quest run 2026-11-11 #19: the dog's bowl read as a flat red disc on the floor): the bowl stands on a feeding mat with
            // the pet's picture - the "this is where the dog eats" corner reads from across the room (visual only, no collider)
            if (_bowl != null && _bowl.Col != null)
            {
                var b = _bowl.Col.bounds; var mat = new GameObject("feeding_mat").transform; mat.SetParent(room.transform, true);
                mat.position = new Vector3(b.center.x, b.min.y + 0.001f, b.center.z);
                var toBed = transform.position - mat.position; toBed.y = 0f; mat.rotation = toBed.sqrMagnitude > 0.01f ? Quaternion.LookRotation(toBed) : Quaternion.identity;
                float mw = Mathf.Max(0.3f, b.size.x + 0.12f);
                Prim.Box(mat, "mat", new Vector3(0, 0, -0.08f), new Vector3(mw, 0.002f, mw + 0.16f), Mats.Lit(kind == PetKind.Cat ? Palette.Purple : Palette.Teal), false);
                var pic = UiKit.Quad(mat, "pet_pic", UiKit.Cutout(UiKit.Tex(kind == PetKind.Cat ? "icon_pet_hungry_cat" : "icon_pet_hungry_dog")), new Vector3(0, 0.0025f, -mw * 0.5f - 0.03f), new Vector2(0.12f, 0.12f));
                pic.transform.localRotation = Quaternion.Euler(90f, 180f, 0f);
                foreach (var r in mat.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            // badge: painted icon + hunger bar, billboarded over the pet
            _badge = new GameObject("hunger_badge").transform; _badge.SetParent(transform, false); _badge.localPosition = new Vector3(0, kind == PetKind.Cat ? 0.62f : 0.82f, 0);
            _hungry = UiKit.Cutout(UiKit.Tex(kind == PetKind.Cat ? "icon_pet_hungry_cat" : "icon_pet_hungry_dog")); _fed = UiKit.Cutout(UiKit.Tex("icon_pet_fed")); _alert = UiKit.Cutout(UiKit.Tex("icon_pet_alert"));
            _icon = UiKit.Quad(_badge, "icon", _hungry, Vector3.zero, new Vector2(0.2f, 0.2f)).GetComponent<Renderer>();
            _barBack = Prim.Box(_badge, "bar_back", new Vector3(0, -0.13f, 0.001f), new Vector3(0.2f, 0.028f, 0.004f), Mats.Unlit(new Color(0.16f, 0.08f, 0.22f)), false).GetComponent<Renderer>();
            _bar = Prim.Box(_badge, "bar", new Vector3(0, -0.13f, 0f), new Vector3(0.18f, 0.018f, 0.005f), Mats.Unlit(Palette.Yellow), false).GetComponent<Renderer>();
            _label = WorldText.Make(_badge, "label", "", new Vector3(0, 0.15f, 0), 0.5f, Palette.Cream);
            _nextWhine = Time.time + 5f;
        }

        float Hunger { get { var r = GameServices.Round; return r != null ? r.PetHunger[_idx] : 0f; } }

        void Update()
        {
            var round = GameServices.Round; if (round == null || _badge == null) return;
            float h = Hunger; bool hungry = h >= RoundController.HungryAt, starving = h >= RoundController.StarvingAt;
            // badge
            var cam = Camera.main; if (cam != null) _badge.rotation = Quaternion.LookRotation(_badge.position - cam.transform.position, Vector3.up);
            // V12 (Quest run: the warning triangle filled the view while I fed the dog): hidden within 0.8 m, smaller up close
            float camD = cam != null ? Vector3.Distance(cam.transform.position, _badge.position) : 9f;
            _badge.localScale = Vector3.one * Mathf.Clamp(camD / 2.5f, 0.4f, 1f);
            bool show = (h > 38f || _fedFlash > 0f) && camD > 0.8f; if (_badge.gameObject.activeSelf != show) _badge.gameObject.SetActive(show);
            if (show)
            {
                _fedFlash -= Time.deltaTime;
                _icon.sharedMaterial = _fedFlash > 0f ? _fed : starving ? _alert : _hungry;
                float pulse = hungry ? 1f + 0.12f * Mathf.Sin(Time.time * (starving ? 12f : 6f)) : 1f; _icon.transform.localScale = new Vector3(0.2f * pulse, 0.2f * pulse, 1f);
                float k = Mathf.Clamp01(h / 100f); _bar.transform.localScale = new Vector3(0.18f * k, 0.018f, 0.005f); _bar.transform.localPosition = new Vector3(-0.09f + 0.09f * k, -0.13f, 0f);
                _bar.sharedMaterial = Mats.Unlit(starving ? Palette.Danger : hungry ? Palette.Coral : Palette.Yellow);
                _label.text = _fedFlash > 0f ? "yum!" : starving ? (Kind == PetKind.Cat ? "MEOW!!" : "WOOF!!") : hungry ? "hungry" : "";
            }
            // every peer: voice the replicated pet noise events (the host's journaled whines)
            foreach (var e in round.RecentNoise)
            {
                if (e.Id <= _seenNoise || e.Cause != NoiseCause.PetAction || e.DirectActor != ActorId) continue;
                _seenNoise = e.Id; if (GameServices.IsAuthority) continue;   // the host voiced it when it submitted it
                Voice(e.Value);
            }
        }

        void Voice(float value)
        {
            var at = Body != null ? Body.transform.position + Vector3.up * 0.3f : transform.position;
            // V13 (owner: "a lot of repetition"): every whine is still voiced (it is real noise for Mom), but a soft whine is a huff and only
            // a starving pet barks / yowls at full voice
            string key = Kind == PetKind.Cat ? "cat_meow" : value > 7f ? "dog_bark" : "dog_huff";
            if (AudioBank.I != null) AudioBank.I.Play(key, at, Mathf.Clamp01(0.35f + value / 14f) * (value > 7f ? 1f : 0.75f), 0.1f);
            RoomFx.Say(at + Vector3.up * 0.4f, Kind == PetKind.Cat ? (value > 7f ? "MEOOOW!" : "meow...") : (value > 7f ? "WOOF! WOOF!" : "whine..."), value > 7f ? Palette.Danger : Palette.Coral, 1.2f, 0.6f);
        }

        void FixedUpdate()
        {
            var round = GameServices.Round;
            if (!GameServices.IsAuthority || round == null || !round.IsPlayable || Body == null) return;
            float h = Hunger;
            // the brain heads for the bowl where it really is (players may have moved it)
            if (_bowl != null && Time.frameCount % 25 == 0)
            {
                var bp = VisualFactory.C(_room.transform.InverseTransformPoint(_bowl.transform.position)); bp.y = 0f;
                if (Kind == PetKind.Cat) _room.Def.CatBowl = bp; else _room.Def.DogBowl = bp;
            }
            // the brain steps up to the bowl once food is in it AND nobody is pouring into it any more (a pet in the stream wastes it)
            if (_bowlFood != null && _bowlFood.Received > _lastReceived + 0.0002f) { _lastReceived = _bowlFood.Received; _fillingUntil = Time.time + 0.8f; }
            round.BowlHasFood[_idx] = _bowlFood != null && _bowlFood.Amount > 0.004f && Time.time > _fillingUntil;
            // eat what is really in the bowl, when standing at it
            if (_bowlFood != null && _bowlFood.Amount > 0.0003f && h > 3f)
            {
                var d = _bowl.transform.position - Body.transform.position; d.y = 0f;
                if (d.magnitude < 0.55f)
                {
                    float take = Mathf.Min(_bowlFood.Amount, 0.028f * Time.fixedDeltaTime);   // 28 g/s, crunch crunch
                    _bowlFood.Amount -= take; _eatAcc += take * 1000f; _lastEatTime = Time.time;
                    if (Time.time > _eatFlushAt) { _eatFlushAt = Time.time + 0.6f; if (AudioBank.I != null) AudioBank.I.Play("rustle", _bowl.transform.position, 0.25f, 0.2f); }
                }
            }
            if (_eatAcc > 0f && (_eatAcc >= 25f || Time.time - _lastEatTime > 0.5f))
            {
                int meals = round.PetMeals[_idx];
                round.PetEat(ActorId, _eatAcc, _bowlFood != null ? _bowlFood.LastFeeder : ActorIds.None); EatenGrams += _eatAcc; _eatAcc = 0f;
                if (round.PetMeals[_idx] > meals) { _fedFlash = 2.5f; RoomFx.Burst("sparkle", Body.transform.position + Vector3.up * 0.4f, 0.2f, 1.5f); }
            }
            // hungry: whine / bark, louder and more often the hungrier - Mom hears pets the family forgot
            // V13 ("calmer Mom"; an unfed cat woke her every ~30 s all match): a pet Mom just calmed (after her visit) stays quiet for the
            // calm time, and a starving pet cries every 6 s at most (was 4 s) - forgetting the pets still wakes her, just not in a loop
            bool calm = round.PetCalmUntil(ActorId) > round.Time;
            if (h >= RoundController.HungryAt && Time.time >= _nextWhine && !calm)
            {
                float t = Mathf.InverseLerp(RoundController.HungryAt, 100f, h);
                _nextWhine = Time.time + Mathf.Lerp(12f, 6f, t);
                float value = Mathf.Lerp(2.5f, 9f, t); var p = Body.transform.position;
                var e = round.SubmitPetSound(ActorId, value, VisualFactory.C(p));
                if (e != null) { Whines++; _seenNoise = Mathf.Max(_seenNoise, e.Id); Voice(value); }
            }
        }
    }
}
