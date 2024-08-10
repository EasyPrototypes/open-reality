﻿using System.Linq;
using Content.Client.Humanoid;
using Content.Shared._White.Wizard.Mirror;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Preferences;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client._White.Wizard.Mirror;

[GenerateTypedNameReferences]
public sealed partial class WizardMirrorWindow : DefaultWindow
{
    private LineEdit NameEdit => CNameEdit;

    private Button SaveButton => CSaveButton;
    public Action<HumanoidCharacterProfile>? OnSave;

    private OptionButton SexButton => CSexButton;

    private LineEdit AgeEdit => CAgeEdit;

    private OptionButton GenderButton => CPronounsButton;

    // TODO: [WD] Uncomment after TTS System port
    //private OptionButton VoiceButton => CVoiceButton;

    // TODO: [WD] Uncomment after BodyType System port
    //private OptionButton BodyTypesButton => CBodyTypesButton;

    private OptionButton SpeciesButton => CSpeciesButton;

    private Slider SkinColorSlider => CSkin;
    private BoxContainer RgbSkinColorContainer => CRgbSkinColorContainer;
    private ColorSelectorSliders _rgbSkinColorSelector;

    // Hair
    private SingleMarkingPicker HairPicker => CHairStylePicker;
    private SingleMarkingPicker FacialHairPicker => CFacialHairPicker;

    private EyeColorPicker EyesPicker => CEyeColorPicker;

    private bool _isDirty;
    public HumanoidCharacterProfile? Profile;

    private readonly MarkingManager _markingManager;
    private readonly IPrototypeManager _prototypeManager;

    // TODO: [WD] Uncomment after bodytype system port
    //private List<BodyTypePrototype> _bodyTypesList = new();

    private readonly List<SpeciesPrototype> _speciesList;

    // TODO: [WD] Uncomment after TTS system port
    //private List<TTSVoicePrototype> _voiceList = default!;

    private const string AnySexVoiceProto = "SponsorAnySexVoices";

    public WizardMirrorWindow(IPrototypeManager prototypeManager)
    {
        RobustXamlLoader.Load(this);

        _markingManager = IoCManager.Resolve<MarkingManager>();
        _prototypeManager = prototypeManager;

        Profile ??= HumanoidCharacterProfile.RandomWithSpecies();

        SaveButton.OnPressed += _ => OnSave!(Profile);

        /* TODO: [WD] Uncomment after TTS system port
        _voiceList = _prototypeManager.EnumeratePrototypes<TTSVoicePrototype>().Where(o => o.RoundStart).ToList();

        #region Voice

        VoiceButton.OnItemSelected += args =>
        {
            VoiceButton.SelectId(args.Id);
            SetVoice(_voiceList[args.Id].ID);
        };

        #endregion
        */

        #region Name

        NameEdit.OnTextChanged += args => { SetName(args.Text); };

        #endregion

        #region Age

        AgeEdit.OnTextChanged += args =>
        {
            if (!int.TryParse(args.Text, out var newAge))
                return;
            SetAge(newAge);
        };

        #endregion Age

        #region Sex

        SexButton.OnItemSelected += args =>
        {
            SexButton.SelectId(args.Id);
            SetSex((Sex) args.Id);
        };

        #endregion Sex

        #region Gender

        GenderButton.AddItem(Loc.GetString("humanoid-profile-editor-pronouns-male-text"), (int) Gender.Male);
        GenderButton.AddItem(Loc.GetString("humanoid-profile-editor-pronouns-female-text"), (int) Gender.Female);
        GenderButton.AddItem(Loc.GetString("humanoid-profile-editor-pronouns-epicene-text"), (int) Gender.Epicene);
        GenderButton.AddItem(Loc.GetString("humanoid-profile-editor-pronouns-neuter-text"), (int) Gender.Neuter);

        GenderButton.OnItemSelected += args =>
        {
            GenderButton.SelectId(args.Id);
            SetGender((Gender) args.Id);
        };

        #endregion Gender

        /* TODO: [WD] Uncomment after bodytype system port
        #region Body Type

        BodyTypesButton.OnItemSelected += OnBodyTypeSelected;

        UpdateBodyTypes();

        #endregion Body Type
        */

        #region Skin


        SkinColorSlider.OnValueChanged += _ =>
        {
            OnSkinColorOnValueChanged();
        };

        RgbSkinColorContainer.AddChild(_rgbSkinColorSelector = new ColorSelectorSliders());
        _rgbSkinColorSelector.OnColorChanged += _ =>
        {
            OnSkinColorOnValueChanged();
        };

        #endregion

        #region Species

        _speciesList = prototypeManager.EnumeratePrototypes<SpeciesPrototype>().Where(o => o.RoundStart).ToList();

        for (var i = 0; i < _speciesList.Count; i++)
        {
            var specie = _speciesList[i];
            var name = Loc.GetString(specie.Name);

            SpeciesButton.AddItem(name, i);
        }

        SpeciesButton.OnItemSelected += args =>
        {
            SpeciesButton.SelectId(args.Id);
            SetSpecies(_speciesList[args.Id].ID);
            UpdateHairPickers();
            OnSkinColorOnValueChanged();
        };

        #endregion Species

        #region Hair

        HairPicker.OnMarkingSelect += newStyle =>
            {
                if (Profile is null)
                    return;

                Profile = Profile.WithCharacterAppearance(
                    Profile.Appearance.WithHairStyleName(newStyle.id));
                IsDirty = true;
            };

        HairPicker.OnColorChanged += newColor =>
            {
                if (Profile is null)
                    return;
                Profile = Profile.WithCharacterAppearance(
                    Profile.Appearance.WithHairColor(newColor.marking.MarkingColors[0]));
                UpdateCMarkingsHair();
                IsDirty = true;
            };

        FacialHairPicker.OnMarkingSelect += newStyle =>
            {
                if (Profile is null)
                    return;
                Profile = Profile.WithCharacterAppearance(
                    Profile.Appearance.WithFacialHairStyleName(newStyle.id));
                IsDirty = true;
            };

        FacialHairPicker.OnColorChanged += newColor =>
            {
                if (Profile is null)
                    return;
                Profile = Profile.WithCharacterAppearance(
                    Profile.Appearance.WithFacialHairColor(newColor.marking.MarkingColors[0]));
                UpdateCMarkingsFacialHair();
                IsDirty = true;
            };

            HairPicker.OnSlotRemove += _ =>
            {
                if (Profile is null)
                    return;
                Profile = Profile.WithCharacterAppearance(
                    Profile.Appearance.WithHairStyleName(HairStyles.DefaultHairStyle)
                );
                UpdateHairPickers();
                UpdateCMarkingsHair();
                IsDirty = true;
            };

            FacialHairPicker.OnSlotRemove += _ =>
            {
                if (Profile is null)
                    return;
                Profile = Profile.WithCharacterAppearance(
                    Profile.Appearance.WithFacialHairStyleName(HairStyles.DefaultFacialHairStyle)
                );
                UpdateHairPickers();
                UpdateCMarkingsFacialHair();
                IsDirty = true;
            };

            HairPicker.OnSlotAdd += delegate
            {
                if (Profile is null)
                    return;

                var hair = _markingManager.MarkingsByCategoryAndSpecies(MarkingCategories.Hair, Profile.Species)
                    .Keys
                    .FirstOrDefault();

                if (string.IsNullOrEmpty(hair))
                    return;

                Profile = Profile.WithCharacterAppearance(
                    Profile.Appearance.WithHairStyleName(hair)
                );

                UpdateHairPickers();
                UpdateCMarkingsHair();
                IsDirty = true;
            };

            FacialHairPicker.OnSlotAdd += delegate
            {
                if (Profile is null)
                    return;

                var hair = _markingManager.MarkingsByCategoryAndSpecies(MarkingCategories.FacialHair, Profile.Species)
                    .Keys
                    .FirstOrDefault();

                if (string.IsNullOrEmpty(hair))
                    return;

                Profile = Profile.WithCharacterAppearance(
                    Profile.Appearance.WithFacialHairStyleName(hair)
                );

                UpdateHairPickers();
                UpdateCMarkingsFacialHair();
                IsDirty = true;
            };

            #endregion Hair

        #region Eyes

        EyesPicker.OnEyeColorPicked += newColor =>
        {
            if (Profile is null)
                return;
            Profile = Profile.WithCharacterAppearance(
                Profile.Appearance.WithEyeColor(newColor));
            IsDirty = true;
        };

        #endregion Eyes

        #region Markings

        CMarkings.OnMarkingAdded += OnMarkingChange;
        CMarkings.OnMarkingRemoved += OnMarkingChange;
        CMarkings.OnMarkingColorChange += OnMarkingChange;
        CMarkings.OnMarkingRankChange += OnMarkingChange;

        #endregion Markings
    }

    #region Set

    private void SetAge(int newAge)
    {
        Profile = Profile?.WithAge(newAge);
        IsDirty = true;
    }

    private void SetSex(Sex newSex)
    {
        Profile = Profile?.WithSex(newSex);
        switch (newSex)
        {
            case Sex.Male:
                Profile = Profile?.WithGender(Gender.Male);
                break;
            case Sex.Female:
                Profile = Profile?.WithGender(Gender.Female);
                break;
            default:
                Profile = Profile?.WithGender(Gender.Epicene);
                break;
        }
        UpdateGenderControls();

        // TODO: [WD] Uncomment after TTS System port
        //UpdateTtsVoicesControls();
        IsDirty = true;
    }

    /* TODO: [WD] Uncomment after TTS system port
    private void SetVoice(string newVoice)
    {
        Profile = Profile?.WithVoice(newVoice);
        IsDirty = true;
    }
    */

    private void SetGender(Gender newGender)
    {
        Profile = Profile?.WithGender(newGender);
        IsDirty = true;
    }

    private void SetSpecies(string newSpecies)
    {
        Profile = Profile?.WithSpecies(newSpecies);
        OnSkinColorOnValueChanged();
        UpdateSexControls();
        // TODO: [WD] Uncomment after BodyType system port
        //UpdateBodyTypes();
        IsDirty = true;
    }

    private void SetName(string newName)
    {
        Profile = Profile?.WithName(newName);
        IsDirty = true;
    }

    /* TODO: [WD] Uncomment after bodytype system port
    private void SetBodyType(string newBodyType)
    {
        Profile = Profile?.WithBodyType(newBodyType);
        IsDirty = true;
    }
    */

    private void OnMarkingChange(MarkingSet markings)
    {
        if (Profile is null)
            return;

        Profile = Profile.WithCharacterAppearance(Profile.Appearance.WithMarkings(markings.GetForwardEnumerator().ToList()));
        IsDirty = true;
    }

    #endregion

    #region Update

    private void UpdateSaveButton()
    {
        SaveButton.Disabled = Profile is null || !IsDirty;
    }

    private void UpdateNamesEdit()
    {
        NameEdit.Text = Profile?.Name ?? "";
    }

    private void UpdateGenderControls()
    {
        if (Profile == null)
            return;

        GenderButton.SelectId((int) Profile.Gender);
    }

    /* TODO: [WD] Uncomment after TTS system port
    private void UpdateTtsVoicesControls()
    {
        if (Profile is null)
            return;

        var sponsorsManager = IoCManager.Resolve<SponsorsManager>();

        VoiceButton.Clear();

        var firstVoiceChoiceId = 1;
        for (var i = 0; i < _voiceList.Count; i++)
        {
            var voice = _voiceList[i];
            if (!HumanoidCharacterProfile.CanHaveVoice(voice, Profile.Sex))
            {
                if (!sponsorsManager.TryGetInfo(out var sponsorInfo)
                    || !sponsorInfo.AllowedMarkings.Contains(AnySexVoiceProto))
                    continue;
            }

            var name = Loc.GetString(voice.Name);
            VoiceButton.AddItem(name, i);

            if (firstVoiceChoiceId == 1)
                firstVoiceChoiceId = i;

            if (voice.SponsorOnly &&
                sponsorsManager.TryGetInfo(out var sponsor) &&
                !sponsor.AllowedMarkings.Contains(voice.ID))
            {
                VoiceButton.SetItemDisabled(i, true);
            }
        }

        var voiceChoiceId = _voiceList.FindIndex(x => x.ID == Profile.Voice);
        if (!VoiceButton.TrySelectId(voiceChoiceId) &&
            VoiceButton.TrySelectId(firstVoiceChoiceId))
        {
            SetVoice(_voiceList[firstVoiceChoiceId].ID);
        }
    }
    */

    private void UpdateSexControls()
    {
        if (Profile == null)
            return;

        SexButton.Clear();

        var sexes = new List<Sex>();

        if (!_prototypeManager.TryIndex<SpeciesPrototype>(Profile.Species, out var speciesProto))
            sexes.Add(Sex.Unsexed);
        else
            sexes.AddRange(speciesProto.Sexes);

        foreach (var sex in sexes)
        {
            SexButton.AddItem(Loc.GetString($"humanoid-profile-editor-sex-{sex.ToString().ToLower()}-text"), (int) sex);
        }

        if (sexes.Contains(Profile.Sex))
            SexButton.SelectId((int) Profile.Sex);
        else
            SexButton.SelectId((int) sexes[0]);
    }

    /* TODO: [WD] Uncomment after bodytype system port
    private void UpdateBodyTypes()
    {
        if (Profile is null)
            return;

        BodyTypesButton.Clear();
        var species = _prototypeManager.Index<SpeciesPrototype>(Profile.Species);
        var sex = Profile.Sex;
        _bodyTypesList = EntitySystem.Get<HumanoidAppearanceSystem>().GetValidBodyTypes(species, sex);

        for (var i = 0; i < _bodyTypesList.Count; i++)
        {
            BodyTypesButton.AddItem(Loc.GetString(_bodyTypesList[i].Name), i);
        }

        if (!_bodyTypesList.Select(proto => proto.ID).Contains(Profile.BodyType.Id))
            SetBodyType(_bodyTypesList.First().ID);

        BodyTypesButton.Select(_bodyTypesList.FindIndex(x => x.ID == Profile.BodyType));
        IsDirty = true;
    }
    */

    private void UpdateHairPickers()
    {
        if (Profile == null)
            return;

        var hairMarking = Profile.Appearance.HairStyleId switch
        {
            HairStyles.DefaultHairStyle => new List<Marking>(),
            _ => new List<Marking> { new(Profile.Appearance.HairStyleId, new List<Color> { Profile.Appearance.HairColor }) },
        };

        var facialHairMarking = Profile.Appearance.FacialHairStyleId switch
        {
            HairStyles.DefaultFacialHairStyle => new List<Marking>(),
            _ => new List<Marking> { new(Profile.Appearance.FacialHairStyleId, new List<Color> { Profile.Appearance.FacialHairColor }) },
        };

        HairPicker.UpdateData(hairMarking, Profile.Species, 1);
        FacialHairPicker.UpdateData(facialHairMarking, Profile.Species, 1);
    }

    private void UpdateCMarkingsFacialHair()
    {
        if (Profile == null)
            return;

        Color? facialHairColor = null;
        if (!(Profile.Appearance.FacialHairStyleId != HairStyles.DefaultFacialHairStyle &&
              _markingManager.Markings.TryGetValue(Profile.Appearance.FacialHairStyleId, out var facialHairProto)))
        {
            return;
        }

        if (_markingManager.CanBeApplied(Profile.Species, Profile.Sex, facialHairProto, _prototypeManager))
        {
            /* TODO: [WD] Uncomment and replace after BodyType system port
             facialHairColor = _markingManager.MustMatchSkin(Profile.BodyType, HumanoidVisualLayers.Hair, out _, _prototypeManager)
             ? Profile.Appearance.SkinColor
             : Profile.Appearance.FacialHairColor;
            */

            facialHairColor = _markingManager.MustMatchSkin(Profile.Species, HumanoidVisualLayers.Hair, out _, _prototypeManager)
                ? Profile.Appearance.SkinColor
                : Profile.Appearance.FacialHairColor;
        }
    }

    private void UpdateCMarkingsHair()
    {
        if (Profile == null)
            return;

        // hair color
        Color? hairColor = null;
        if (!(Profile.Appearance.HairStyleId != HairStyles.DefaultHairStyle &&
              _markingManager.Markings.TryGetValue(Profile.Appearance.HairStyleId, out var hairProto)))
        {
            return;
        }

        if (_markingManager.CanBeApplied(Profile.Species, Profile.Sex, hairProto, _prototypeManager))
        {

            /* TODO: [WD] Uncomment and replace after BodyType system port
            hairColor = _markingManager.MustMatchSkin(Profile.BodyType, HumanoidVisualLayers.Hair, out _, _prototypeManager)
                ? Profile.Appearance.SkinColor
                : Profile.Appearance.HairColor;
            */

            hairColor = _markingManager.MustMatchSkin(Profile.Species, HumanoidVisualLayers.Hair, out _, _prototypeManager)
                ? Profile.Appearance.SkinColor
                : Profile.Appearance.HairColor;
        }
    }

    private void UpdateSkinColor()
    {
        if (Profile == null)
            return;

        var skin = _prototypeManager.Index<SpeciesPrototype>(Profile.Species).SkinColoration;

        switch (skin)
        {
            case HumanoidSkinColor.HumanToned:
            {
                if (!SkinColorSlider.Visible)
                {
                    SkinColorSlider.Visible = true;
                    _rgbSkinColorSelector.Visible = false;
                }

                SkinColorSlider.Value = SkinColor.HumanSkinToneFromColor(Profile.Appearance.SkinColor);

                break;
            }
            case HumanoidSkinColor.Hues:
            {
                if (!_rgbSkinColorSelector.Visible)
                {
                    SkinColorSlider.Visible = false;
                    _rgbSkinColorSelector.Visible = true;
                }

                // set the RGB values to the direct values otherwise
                _rgbSkinColorSelector.Color = Profile.Appearance.SkinColor;
                break;
            }
            case HumanoidSkinColor.TintedHues:
            {
                if (!_rgbSkinColorSelector.Visible)
                {
                    SkinColorSlider.Visible = false;
                    _rgbSkinColorSelector.Visible = true;
                }

                // set the RGB values to the direct values otherwise
                _rgbSkinColorSelector.Color = Profile.Appearance.SkinColor;
                break;
            }
        }
    }

    private void UpdateSpecies()
    {
        if (Profile == null)
        {
            return;
        }

        if (!_speciesList.Exists(x => x.ID == Profile.Species))
        {
            SpeciesButton.Select(0);
            return;
        }

        SpeciesButton.Select(_speciesList.FindIndex(x => x.ID == Profile.Species));
    }

    private void UpdateAgeEdit()
    {
        AgeEdit.Text = Profile?.Age.ToString() ?? "";
    }

    private void UpdateEyePickers()
    {
        if (Profile == null)
        {
            return;
        }

        EyesPicker.SetData(Profile.Appearance.EyeColor);
    }

    private void UpdateMarkings()
    {
        if (Profile == null)
        {
            return;
        }

        /* TODO: [WD] Uncomment and replace after BodyType system port
        CMarkings.SetData(Profile.Appearance.Markings, Profile.Species,
            Profile.Sex, Profile.BodyType, Profile.Appearance.SkinColor, Profile.Appearance.EyeColor
        );
        */

        CMarkings.SetData(Profile.Appearance.Markings, Profile.Species,
            Profile.Sex, Profile.Appearance.SkinColor, Profile.Appearance.EyeColor
        );
    }

    #endregion

    private void OnSkinColorOnValueChanged()
    {
        if (Profile is null)
            return;

        var skin = _prototypeManager.Index<SpeciesPrototype>(Profile.Species).SkinColoration;

        switch (skin)
        {
            case HumanoidSkinColor.HumanToned:
            {
                if (!SkinColorSlider.Visible)
                {
                    SkinColorSlider.Visible = true;
                    RgbSkinColorContainer.Visible = false;
                }

                var color = SkinColor.HumanSkinTone((int) SkinColorSlider.Value);

                Profile = Profile.WithCharacterAppearance(Profile.Appearance.WithSkinColor(color));//
                break;
            }
            case HumanoidSkinColor.Hues:
            {
                if (!RgbSkinColorContainer.Visible)
                {
                    SkinColorSlider.Visible = false;
                    RgbSkinColorContainer.Visible = true;
                }

                Profile = Profile.WithCharacterAppearance(Profile.Appearance.WithSkinColor(_rgbSkinColorSelector.Color));
                break;
            }
            case HumanoidSkinColor.TintedHues:
            {
                if (!RgbSkinColorContainer.Visible)
                {
                    SkinColorSlider.Visible = false;
                    RgbSkinColorContainer.Visible = true;
                }

                var color = SkinColor.TintedHues(_rgbSkinColorSelector.Color);

                Profile = Profile.WithCharacterAppearance(Profile.Appearance.WithSkinColor(color));
                break;
            }
        }

        IsDirty = true;
    }

    /* TODO: Uncomment after BodyType system port
    private void OnBodyTypeSelected(OptionButton.ItemSelectedEventArgs args)
    {
        args.Button.SelectId(args.Id);
        SetBodyType(_bodyTypesList[args.Id].ID);
    }
    */

    private bool IsDirty
    {
        get => _isDirty;
        set
        {
            _isDirty = value;
            UpdateSaveButton();
        }
    }

    public void UpdateState(WizardMirrorUiState state)
    {
        Profile = state.Profile;

        UpdateNamesEdit();
        UpdateSexControls();
        UpdateGenderControls();
        UpdateSkinColor();
        UpdateSpecies();
        UpdateAgeEdit();
        UpdateEyePickers();
        UpdateSaveButton();
        UpdateHairPickers();
        UpdateCMarkingsHair();
        UpdateCMarkingsFacialHair();

        // TODO: [WD] Uncomment after TTS and BodyType systems port
        //UpdateTtsVoicesControls();
        //UpdateBodyTypes();

        UpdateMarkings();
    }
}
