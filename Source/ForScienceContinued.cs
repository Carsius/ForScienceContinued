/**
 *		ForScience
 *		Original code from WaveFunctionP(http://forum.kerbalspaceprogram.com/members/107709)
 *		This code is licensed under the Attribution-ShareAlike 4.0 (CC BY-SA 4.0)
 *		creative commons license. See (http://creativecommons.org/licenses/by-sa/4.0/)
 *		for full details.
 *		Modified by	SpaceTiger(http://forum.kerbalspaceprogram.com/members/137260) and
 *		by	SpaceKitty(http://forum.kerbalspaceprogram.com/members/137262)
 *
 *		Original Thread: http://forum.kerbalspaceprogram.com/threads/76437
 *		Origianl GitHub: https://github.com/WaveFunctionP/ForScience
 *
 *  Modified Github: https://github.com/Xarun/ForScience
 *
 *
 *		to-do:
 *		  fix bugs that i didnt find yet
 */

using KerboKatz.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace KerboKatz
{
  [KSPAddon(KSPAddon.Startup.Flight, false)]
  partial class ForScienceContinued : KerboKatzBase
  {
    private bool IsDataToCollect = false;
    private bool setTo56;
    private CelestialBody currentBody = null;
    private Dictionary<string, double> runningExperiments = new Dictionary<string, double>();
    private Dictionary<string, int> shipCotainsExperiments = new Dictionary<string, int>();
    private Dictionary<string, string> fixedBiomes = new Dictionary<string, string>();
    private double lastFrameCheck;
    private double nextUpdate = 0;
    private ExperimentSituations currentSituation = 0;
    private float CurrentFrame = 0;
    private float frameCheck;
    private KerbalEVA kerbalEVAPart = null;
    private List<KerbalEVA> kerbalEVAParts = null;
    private List<ModuleScienceContainer> containerList = null;
    private List<ModuleScienceExperiment> experimentList = null;
    private List<String> toolbarStrings = new List<String>();
    private ModuleScienceContainer container = null;
    private Vessel currentVessel = null;
    private Vessel parentVessel;
    private bool hasScientist;

    public ForScienceContinued()
    {
      modName = "ForScienceContinued";
      tooltip = "Use left click to turn ForScience on/off.\n Use right click to open the settings menu.";
      requiresUtilities = new Version(1, 2, 5);
    }

    protected override void Started()
    {
      if (!(HighLogic.CurrentGame.Mode == Game.Modes.CAREER || HighLogic.CurrentGame.Mode == Game.Modes.SCIENCE_SANDBOX))
      {
        Destroy(this);
        return;
      }
      if (currentSettings.getBool("Debug"))
      {
        Utilities.debugList.AddUnique(modName);
      }
      else if (Utilities.debugList.Contains(modName))
      {
        Utilities.debugList.Remove(modName);
      }

      currentSettings.setDefault("scienceCutoff", "2");
      currentSettings.setDefault("spriteAnimationFPS", "25");
      currentSettings.setDefault("autoScience", "false");
      currentSettings.setDefault("runOneTimeScience", "false");
      currentSettings.setDefault("transferScience", "false");
      currentSettings.setDefault("showSettings", "false");
      currentSettings.setDefault("doEVAonlyIfOnGroundWhenLanded", "true");
      currentSettings.setDefault("transferAll", "false");
      currentSettings.setDefault("dumpDuplicateResults", "false");
      currentSettings.setDefault("resetExperiments", "false");
      windowPosition.x = currentSettings.getFloat("windowX");
      windowPosition.y = currentSettings.getFloat("windowY");

      resetExperiments = currentSettings.getBool("resetExperiments");
      dumpDuplicateResults = currentSettings.getBool("dumpDuplicateResults");
      transferAll = currentSettings.getBool("transferAll");
      scienceCutoff = currentSettings.getString("scienceCutoff");
      spriteAnimationFPS = currentSettings.getString("spriteAnimationFPS");
      transferScience = currentSettings.getBool("transferScience");
      doEVAonlyIfOnGroundWhenLanded = currentSettings.getBool("doEVAonlyIfOnGroundWhenLanded");
      runOneTimeScience = currentSettings.getBool("runOneTimeScience");

      GameEvents.onCrewOnEva.Add(GoingEva);
      GameEvents.onVesselChange.Add(updateVessel);
      GameEvents.onVesselSituationChange.Add(updateSituation);
      GameEvents.onDominantBodyChange.Add(updateBody);
      setAppLauncherScenes(ApplicationLauncher.AppScenes.FLIGHT);
      updateFrameCheck();
    }

    private void updateSituation(GameEvents.HostedFromToAction<Vessel, Vessel.Situations> data)
    {
      Utilities.debug(modName, Utilities.LogMode.Debug, "updateSituation");
      resetLists();
      currentSituation = ScienceUtil.GetExperimentSituation(currentVessel);
      updateExperimentList();
    }

    private void updateBody(GameEvents.FromToAction<CelestialBody, CelestialBody> data)
    {
      Utilities.debug(modName, Utilities.LogMode.Debug, "updateBody");
      resetLists();
      currentBody = currentVessel.mainBody;
      updateExperimentList();
    }

    private void updateVessel(Vessel data)
    {
      Utilities.debug(modName, Utilities.LogMode.Debug, "updateVessel");

      resetLists();
      hasScientist = false;

      currentVessel = FlightGlobals.ActiveVessel;
      foreach (var crew in currentVessel.GetVesselCrew())
      {
        if (crew.experienceTrait.Title == "Scientist")
        {
          hasScientist = true;
          break;
        }
      }

      updateExperimentList();
    }

    private void resetLists()
    {
      if (shipCotainsExperiments != null)
        shipCotainsExperiments.Clear();
      if (toolbarStrings != null)
        toolbarStrings.Clear();
      if (containerList != null)
        containerList.Clear();
      if (experimentList != null)
        experimentList.Clear();
    }

    private void updateExperimentList()
    {
      experimentList = currentVessel.FindPartModulesImplementing<ModuleScienceExperiment>();
      containerList = currentVessel.FindPartModulesImplementing<ModuleScienceContainer>();
      foreach (ModuleScienceContainer currentContainer in containerList)
      {
        addToExpermientedList(currentContainer.GetData());
        toolbarStrings.Add(currentContainer.part.partInfo.title);
      }
      if ((container == null || container.vessel != currentVessel) && containerList != null && containerList.Count > 0)
        container = containerList[0];
    }

    protected override void OnDestroy()
    {
      GameEvents.onCrewOnEva.Remove(GoingEva);
      GameEvents.onVesselChange.Remove(updateVessel);
      GameEvents.onVesselSituationChange.Remove(updateSituation);
      GameEvents.onDominantBodyChange.Remove(updateBody);
      if (currentSettings != null)
      {
        currentSettings.set("showSettings", false);
      }
      base.OnDestroy();
    }

    private void GoingEva(GameEvents.FromToAction<Part, Part> parts)
    {
      parentVessel = parts.from.vessel;
    }

    public void Update()
    {
      if (currentSettings.getBool("autoScience"))
      {
        if (lastFrameCheck + frameCheck < Time.time)
        {
          var frame = Time.deltaTime / frameCheck;
          if (CurrentFrame + frame < 56 - 1)
            CurrentFrame += frame;
          else
            CurrentFrame = 0;
          setIcon(Utilities.getTexture("icon" + (int)CurrentFrame, "ForScienceContinued/Textures"));
          lastFrameCheck = Time.time;
        }
        if (setTo56)
          setTo56 = false;
      }
      else if (!setTo56)
      {
        setIcon(Utilities.getTexture("icon56", "ForScienceContinued/Textures"));
        setTo56 = true;
      }
    }

    private void updateFrameCheck()
    {
      frameCheck = 1 / currentSettings.getFloat("spriteAnimationFPS");
    }

    protected override void onToolbar()
    {
      if (Input.GetMouseButtonUp(0))
      {//left mouse button
        if (currentSettings.getBool("autoScience"))
        {
          currentSettings.set("autoScience", false);
        }
        else
        {
          currentSettings.set("autoScience", true);
        }
      }
      else if (Input.GetMouseButtonUp(1))//right mouse button
      {
        if (currentSettings.getBool("showSettings"))
        {
          currentSettings.set("showSettings", false);
        }
        else
        {
          //only move window when the position was not set in the settings
          windowPosition.UpdatePosition = Rectangle.updateType.Cursor;
          currentSettings.set("showSettings", true);
        }
      }
      currentSettings.save();
    }

    // void Update()
    private void FixedUpdate()
    {
      if (nextUpdate == 0 && FlightGlobals.ready)
      {//add some delay so it doesnt run as soon as the vehicle launches
        nextUpdate = Planetarium.GetUniversalTime() + 1;
        updateVessel(null);
        updateSituation(new GameEvents.HostedFromToAction<Vessel, Vessel.Situations>());
        updateBody(new GameEvents.FromToAction<CelestialBody, CelestialBody>());
        updateExperimentList();
      }
      if (FlightGlobals.ready &&
          currentSettings.getBool("autoScience") &&
          Planetarium.GetUniversalTime() > nextUpdate)
      {
        nextUpdate = Planetarium.GetUniversalTime() + 1;
        if (Utilities.canVesselBeControlled(currentVessel))
        {
          if (IsDataToCollect && currentSettings.getBool("transferScience") && !currentVessel.isEVA)
            TransferScience();

          RunScience();
        }
      }
    }

    private void TransferScience()
    {
      if (container == null || container.vessel != currentVessel)
        return;
      if (experimentList.Count > 0)
      {
        var scienceContainer = new List<IScienceDataContainer>();
        var containingData = container.GetData();
        foreach (var thisExperiment in experimentList)
        {
          if (canTransfer(thisExperiment, containingData))
          {
            scienceContainer.Add(thisExperiment);
          }
        }
        if (scienceContainer.Count > 0)
          container.StoreData(scienceContainer, currentSettings.getBool("dumpDuplicateResults"));
      }
      IsDataToCollect = false;
    }

    private bool canTransfer(ModuleScienceExperiment thisExperiment, ScienceData[] containingData)
    {
      if (!thisExperiment.IsRerunnable())
      {
        if (currentSettings.getBool("transferAll"))
        {
          return true;
        }
        else
        {
          return false;
        }
      }
      if (!currentSettings.getBool("dumpDuplicateResults"))
      {
        foreach (var data in thisExperiment.GetData())
        {
          if (containingData.Contains(data))
          {
            return false;
          }
        }
      }
      return true;
    }

    private void RunScience()
    {
      CheckEVA();
      foreach (ModuleScienceExperiment currentExperiment in experimentList)
      {
        CheckForDataToCollect(currentExperiment);
        var experiment = ResearchAndDevelopment.GetExperiment(currentExperiment.experimentID);
        var biome = getBiomeForExperiment(experiment);
        var currentScienceSubject = ResearchAndDevelopment.GetExperimentSubject(experiment, currentSituation, currentBody, biome);
        var currentScienceValue = Utilities.Science.getScienceValue(shipCotainsExperiments, experiment, currentScienceSubject);

        if (currentExperiment.part.partInfo.manufacturer == "DMagic Orbital Science")
        {
          if (!canConduct(currentExperiment, experiment, currentScienceValue))
          {//check if dmagic expriment can be conducted
            continue;
          }
        }
        else if (!canRunExperiment(currentExperiment, experiment, currentScienceValue))
        {
          continue;
        }
        if (isExperimentLimitReached(currentExperiment, experiment, currentScienceSubject, ref currentScienceValue))
        {//check if dmagic experimentLimit is reached
          continue;
        }
        if (DataInContainer(currentScienceSubject, currentExperiment.GetData()))
        {
          continue;
        }
        Utilities.debug(modName, "Deploying: " + currentScienceSubject.id + "\nScience: " + currentScienceValue);

        DeployExperiment(currentExperiment);

        addToContainer(currentScienceSubject.id);
        if (canResetExperiment(currentExperiment))
        {
          ResetExperiment(currentExperiment);
        }
      }
    }

    private bool canResetExperiment(ModuleScienceExperiment currentExperiment)
    {
      if (!hasScientist)
        return false;
      if (!currentSettings.getBool("resetExperiments"))
        return false;
      if (!currentExperiment.Inoperable)
        return false;
      return true;
    }

    private void CheckForDataToCollect(ModuleScienceExperiment currentExperiment)
    {
      if (IsDataToCollect)
        return;
      if (!currentSettings.getBool("transferScience"))
        return;
      if (currentExperiment.Deployed && currentExperiment.dataIsCollectable)
      {
        if (currentExperiment.IsRerunnable() && !currentSettings.getBool("transferAll"))
          return;
        IsDataToCollect = true;
      }
    }

    private void DeployExperiment(ModuleScienceExperiment currentExperiment)
    {
      try//taken from ScienceAlert since .DeployExperiment() didnt work and i didnt know about this
      {
        // Get the most-derived type and use its DeployExperiment so we don't
        // skip any plugin-derived versions
        currentExperiment.GetType().InvokeMember("DeployExperiment", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.IgnoreReturn | System.Reflection.BindingFlags.InvokeMethod, null, currentExperiment, null);
      }
      catch (Exception e)
      {
        Utilities.debug(modName, Utilities.LogMode.Error, "Failed to invoke \"DeployExperiment\" using GetType(), falling back to base type after encountering exception " + e);
        currentExperiment.DeployExperiment();
      }
    }

    private void ResetExperiment(ModuleScienceExperiment currentExperiment)
    {//Same as DeployExperiment just switched out the name
      try
      {
        currentExperiment.GetType().InvokeMember("ResetExperiment", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.IgnoreReturn | System.Reflection.BindingFlags.InvokeMethod, null, currentExperiment, null);
      }
      catch (Exception e)
      {
        Utilities.debug(modName, Utilities.LogMode.Error, "Failed to invoke \"ResetExperiment\" using GetType(), falling back to base type after encountering exception " + e);
        currentExperiment.ResetExperiment();
      }
      currentExperiment.Inoperable = false;
    }

    #region try-catch for DMagic Orbital Science //thanks Sephiroth018 for help on this part
    private bool isExperimentLimitReached(ModuleScienceExperiment currentExperiment, ScienceExperiment experiment, ScienceSubject currentScienceSubject, ref float currentScienceValue)
    {
      try
      {
        int experimentNumber, experimentLimit;
        if (int.TryParse(currentExperiment.GetType().GetField("experimentNumber").GetValue(currentExperiment).ToString(), out  experimentNumber) &&
            int.TryParse(currentExperiment.GetType().GetField("experimentLimit").GetValue(currentExperiment).ToString(), out  experimentLimit))
        {
          if ((experimentNumber >= experimentLimit) && experimentLimit >= 1)
          {
            Utilities.debug(modName, Utilities.LogMode.Log, "Experiment {0} can't be conducted cause the experimentLimit is reached!", experiment.id);
            return true;
          }
          else if (experimentNumber > 0)
          {
            addToContainer(currentScienceSubject.id, experimentNumber);
            currentScienceValue = Utilities.Science.getScienceValue(shipCotainsExperiments, experiment, currentScienceSubject);
            Utilities.debug(modName, Utilities.LogMode.Log, "Experiment is a DMagic Orbital Science experiment. Science value changed to: " + currentScienceValue);
            if (currentScienceValue < currentSettings.getFloat("scienceCutoff"))
            {
              Utilities.debug(modName, Utilities.LogMode.Log, "Experiment is a DMagic Orbital Science experiment. Science value droped below cutoff.");
              return true;
            }
          }
        }
      }
      catch (Exception)
      {
      }
      return false;
    }

    private bool canConduct(ModuleScienceExperiment currentExperiment, ScienceExperiment experiment, float currentScienceValue)
    {
      try
      {
        MethodInfo conductMethod = currentExperiment.GetType().BaseType.GetMethod("canConduct", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy | BindingFlags.InvokeMethod);
        if (conductMethod == null)
        {
          conductMethod = currentExperiment.GetType().GetMethod("canConduct", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy | BindingFlags.InvokeMethod);
        }
        if (conductMethod != null)
        {
          var conductResult = (bool)conductMethod.Invoke(currentExperiment, null);

          if (!conductResult)
          {
            Utilities.debug(modName, Utilities.LogMode.Debug, "Experiment {0} can't be conducted.", experiment.id);
            return false;
          }
          if (canRunExperiment(currentExperiment, experiment, currentScienceValue, true))
          {
            Utilities.debug(modName, Utilities.LogMode.Debug, "Experiment {0} can be run.", experiment.id);
            return true;
          }
        }
        else
        {
          Utilities.debug(modName, Utilities.LogMode.Error, experiment.id + ": conductMethod == null");
        }
      }
      catch (Exception e)
      {
        Utilities.debug(modName, Utilities.LogMode.Exception, e.Message);
      }
      return false;
    }

    #endregion try-catch for DMagic Orbital Science //thanks Sephiroth018 for help on this part
    private string getBiomeForExperiment(ScienceExperiment experiment)
    {
      if (experiment.BiomeIsRelevantWhile(currentSituation))
      {
        return currentBiome();
      }
      return string.Empty;
    }

    private bool canRunExperiment(ModuleScienceExperiment currentExperiment, ScienceExperiment experiment, float currentScienceValue, bool DMagic = false)
    {
      if (!DMagic)
      {
        if (!experiment.IsAvailableWhile(currentSituation, currentBody))//
        {
          //experiment.mask
          Utilities.debug(modName, Utilities.LogMode.Debug, currentExperiment.experimentID + ": Experiment isn't available in the current situation: " + currentSituation + "_" + currentBody + "_" + experiment.situationMask);
          return false;
        }
        if (currentExperiment.Inoperable)//
        {
          Utilities.debug(modName, Utilities.LogMode.Debug, currentExperiment.experimentID + ": Experiment is inoperable");
          return false;
        }
        if (currentExperiment.Deployed)//
        {
          Utilities.debug(modName, Utilities.LogMode.Debug, currentExperiment.experimentID + ": Experiment is deployed");
          return false;
        }
      }
      if (!currentExperiment.rerunnable && !currentSettings.getBool("runOneTimeScience"))
      {
        Utilities.debug(modName, Utilities.LogMode.Debug, currentExperiment.experimentID + ": Runing rerunable experiments is disabled");
        return false;
      }
      if (currentScienceValue < currentSettings.getFloat("scienceCutoff"))
      {
        Utilities.debug(modName, Utilities.LogMode.Debug, currentExperiment.experimentID + ": Science value is less than cutoff threshold: " + currentScienceValue + "<" + currentSettings.getFloat("scienceCutoff"));
        return false;
      }
      if (!experiment.IsUnlocked())
      {
        Utilities.debug(modName, Utilities.LogMode.Debug, currentExperiment.experimentID + ": Experiment is locked");
        return false;
      }

      var ModuleAnimateGeneric = currentExperiment.part.FindModuleImplementing<ModuleAnimateGeneric>();
      if (ModuleAnimateGeneric != null)
      {
        if (ModuleAnimateGeneric.status != "Locked")
        {
          Utilities.debug(modName, Utilities.LogMode.Debug, "Animation status isn't locked:" + ModuleAnimateGeneric.status + "_" + currentExperiment.part.name);
          return false;
        }
      }
      return true;
    }

    private void CheckEVA()
    {
      if (currentVessel.isEVA && kerbalEVAPart == null)
      {
        kerbalEVAParts = currentVessel.FindPartModulesImplementing<KerbalEVA>();
        kerbalEVAPart = kerbalEVAParts.First();
      }

      if (currentVessel.isEVA && currentSettings.getBool("doEVAonlyIfOnGroundWhenLanded") && (parentVessel.Landed || parentVessel.Splashed) && (kerbalEVAPart.OnALadder || (!currentVessel.Landed && !currentVessel.Splashed)))
      {
        return;
      }
    }

    private bool DataInContainer(ScienceSubject ScienceSubject, ScienceData[] ScienceData)
    {
      foreach (ScienceData data in ScienceData)
      {
        if (ScienceSubject.id.Contains(data.subjectID))
        {
          return true;
        }
      }
      return false;
    }

    private void addToExpermientedList(ScienceData[] data)
    {
      foreach (ScienceData currentData in data)
      {
        addToContainer(currentData.subjectID);
      }
    }

    private void addToContainer(string subjectID, int add = 0)
    {
      if (shipCotainsExperiments.ContainsKey(subjectID))
      {
        Utilities.debug(modName, Utilities.LogMode.Debug, subjectID + "_Containing");
        shipCotainsExperiments[subjectID] = shipCotainsExperiments[subjectID] + 1 + add;
      }
      else
      {
        Utilities.debug(modName, Utilities.LogMode.Debug, subjectID + "_New");
        shipCotainsExperiments.Add(subjectID, add + 1);
      }
    }

    private string currentBiome()
    {
      if (currentVessel != null && currentBody != null)
      {
        if (!string.IsNullOrEmpty(FlightGlobals.ActiveVessel.landedAt))
        {
          //big thanks to xEvilReeperx for this one.
          return Vessel.GetLandedAtString(FlightGlobals.ActiveVessel.landedAt);
        }
        else
        {
          return ScienceUtil.GetExperimentBiome(currentBody, currentVessel.latitude, currentVessel.longitude);
        }
      }
      else
      {
        Utilities.debug(modName, Utilities.LogMode.Warning, "currentVessel && currentBody == null");
      }
      return string.Empty;
    }
  }
}