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
using UnityEngine;

namespace KerboKatz
{
  [KSPAddon(KSPAddon.Startup.Flight, false)]
  partial class ForScienceContinued : KerboKatzBase
  {
    private bool dataIsInContainer = false;
    private bool IsDataToCollect = false;
    private CelestialBody currentBody = null;
    private Dictionary<string, double> runningExperiments = new Dictionary<string, double>();
    private Dictionary<string, int> shipCotainsExperiments = new Dictionary<string, int>();
    private double lastUpdate = 0;
    private ExperimentSituations currentSituation = 0;
    private int experimentLimit;
    private int experimentNumber;
    private KerbalEVA kerbalEVAPart = null;
    private List<KerbalEVA> kerbalEVAParts = null;
    private List<ModuleScienceContainer> containerList = null;
    private List<ModuleScienceExperiment> experimentList = null;
    private List<String> startedExperiments = new List<String>();
    private List<String> toolbarStrings = new List<String>();
    private ModuleScienceContainer container = null;
    private ScienceExperiment experiment;
    private string currentBiome = null;
    private Vessel currentVessel = null;
    private Vessel parentVessel;

    public ForScienceContinued()
    {
      modName = "ForScienceContinued";
      tooltip = "Use left click to turn ForScience on/off.\n Use right click to open the settings menu.";
      requiresUtilities = new Version(1, 2, 0);
    }

    protected override void Started()
    {
      if (!(HighLogic.CurrentGame.Mode == Game.Modes.CAREER || HighLogic.CurrentGame.Mode == Game.Modes.SCIENCE_SANDBOX))
      {
        Destroy(this);
        return;
      }

      currentSettings.setDefault("scienceCutoff", "2");
      currentSettings.setDefault("spriteAnimationFPS", "25");
      currentSettings.setDefault("autoScience", "false");
      currentSettings.setDefault("runOneTimeScience", "false");
      currentSettings.setDefault("transferScience", "false");
      currentSettings.setDefault("showSettings", "false");
      currentSettings.setDefault("doEVAonlyIfOnGroundWhenLanded", "true");
      currentSettings.setDefault("transferAll", "false");
      windowPosition.x = currentSettings.getFloat("windowX");
      windowPosition.y = currentSettings.getFloat("windowY");

      transferAll = currentSettings.getBool("transferAll");
      scienceCutoff = currentSettings.getString("scienceCutoff");
      spriteAnimationFPS = currentSettings.getString("spriteAnimationFPS");
      transferScience = currentSettings.getBool("transferScience");
      doEVAonlyIfOnGroundWhenLanded = currentSettings.getBool("doEVAonlyIfOnGroundWhenLanded");
      runOneTimeScience = currentSettings.getBool("runOneTimeScience");

      GameEvents.onCrewOnEva.Add(GoingEva);

      setAppLauncherScenes(ApplicationLauncher.AppScenes.FLIGHT);
      updateFrameCheck();
    }

    protected override void OnDestroy()
    {
      GameEvents.onCrewOnEva.Remove(GoingEva);
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

    private int FrameCount = 56;
    private float CurrentFrame = 0;
    private double lastFrameCheck;
    private float frameCheck;
    private bool setTo56;
    public void Update()
    {
      if (currentSettings.getBool("autoScience"))
      {
        if (lastFrameCheck + frameCheck < Time.time)
        {
          var frame = Time.deltaTime / frameCheck;
          if (CurrentFrame + frame < FrameCount - 1)
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
      if (lastUpdate == 0)
      {//add some delay so it doesnt run as soon as the vehicle launches
        lastUpdate = Planetarium.GetUniversalTime() + 5;
        UpdateCurrent();
      }
      if (FlightGlobals.ready &&
          currentSettings.getBool("autoScience") &&
          Planetarium.GetUniversalTime() > lastUpdate)
      {
        lastUpdate = Planetarium.GetUniversalTime() + 1;
        UpdateCurrent();
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
      if (container == null)
        return;
      Utilities.debug(modName, "Tranfering science to container.");
      if (currentSettings.getBool("transferAll"))
      {
        container.StoreData(experimentList.Cast<IScienceDataContainer>().ToList(), false);
      }
      else
      {
        if (experimentList.Count > 0)
        {
          var scienceContainer = new List<IScienceDataContainer>();
          foreach (var thisExperiment in experimentList.Cast<IScienceDataContainer>())
          {
            if (thisExperiment.IsRerunnable())
            {
              scienceContainer.Add(thisExperiment);
            }
          }
          container.StoreData(scienceContainer, false);
        }
      }
      IsDataToCollect = false;
    }

    private void RunScience()
    {
      if (!currentSettings.getBool("autoScience"))
        return;
      if (currentVessel.isEVA && kerbalEVAPart == null)
      {
        kerbalEVAParts = currentVessel.FindPartModulesImplementing<KerbalEVA>();
        kerbalEVAPart = kerbalEVAParts.First();
      }

      if (currentVessel.isEVA && currentSettings.getBool("doEVAonlyIfOnGroundWhenLanded") && (parentVessel.Landed || parentVessel.Splashed) && (kerbalEVAPart.OnALadder || (!currentVessel.Landed && !currentVessel.Splashed)))
      {
        return;
      }
      foreach (ModuleScienceExperiment currentExperiment in experimentList)
      {
        addToExpermientedList(currentExperiment.GetData());
        var fixBiome = string.Empty;

        experiment = ResearchAndDevelopment.GetExperiment(currentExperiment.experimentID);

        if (experiment.BiomeIsRelevantWhile(currentSituation))
        {
          fixBiome = currentBiome;
        }
        var currentScienceSubject = ResearchAndDevelopment.GetExperimentSubject(experiment, currentSituation, currentBody, fixBiome);
        float currentScienceValue = Utilities.Science.getScienceValue(shipCotainsExperiments, experiment, currentScienceSubject);
        if (((!currentExperiment.rerunnable && currentSettings.getBool("runOneTimeScience")) || currentExperiment.rerunnable) &&
            experiment.IsAvailableWhile(currentSituation, currentBody) &&
            currentScienceValue >= currentSettings.getFloat("scienceCutoff") &&
            !currentExperiment.Inoperable &&
            !currentExperiment.Deployed &&
            experiment.IsUnlocked() &&
            !(runningExperiments.ContainsKey(experiment.id) && runningExperiments[experiment.id] > lastUpdate))
        {
          if (runningExperiments.ContainsKey(experiment.id))
          {
            runningExperiments.Remove(experiment.id);
          }

          dataIsInContainer = false;
          checkDataInContainer(currentScienceSubject, container.GetData());
          checkDataInContainer(currentScienceSubject, currentExperiment.GetData());

          #region try-catch for DMagic Orbital Science
          try
          {
            var conductMethod = currentExperiment.GetType().GetMethod("conduct");//thanks Sephiroth018 for this conduct part
            if (conductMethod != null)
            {
              Utilities.debug(modName, "Experiment {0} is a DMagic Orbital Science experiment.", experiment.id);

              var conductResult = (bool)conductMethod.Invoke(null, new object[] { currentExperiment });

              if (!conductResult)
              {
                Utilities.debug(modName, "Experiment {0} can't be conducted.", experiment.id);
                continue;
              }
            }
          }
          catch (Exception)
          {
          }
          try
          {
            if (int.TryParse(currentExperiment.GetType().GetField("experimentNumber").GetValue(currentExperiment).ToString(), out  experimentNumber) &&
                int.TryParse(currentExperiment.GetType().GetField("experimentLimit").GetValue(currentExperiment).ToString(), out  experimentLimit))
            {
              if ((experimentNumber >= experimentLimit) && experimentLimit >= 1)
              {
                Utilities.debug(modName, "Experiment {0} can't be conducted cause the experimentLimit is reached!", experiment.id);
                continue;
              }
              else if (experimentNumber > 0)
              {
                if (shipCotainsExperiments.ContainsKey(currentScienceSubject.id))
                  shipCotainsExperiments[currentScienceSubject.id] += experimentNumber;
                else
                  shipCotainsExperiments.Add(currentScienceSubject.id, experimentNumber + 1);
                currentScienceValue = Utilities.Science.getScienceValue(shipCotainsExperiments, experiment, currentScienceSubject);
                Utilities.debug(modName, "Experiment is a DMagic Orbital Science experiment. Science value changed to: " + currentScienceValue);
                if (currentScienceValue < currentSettings.getFloat("scienceCutoff"))
                {
                  Utilities.debug(modName, "Experiment is a DMagic Orbital Science experiment. Science value droped below cutoff.");
                  continue;
                }
              }
            }
          }
          catch (Exception)
          {
          }
          #endregion try-catch for DMagic Orbital Science

          if (!dataIsInContainer)
          {
            Utilities.debug(modName, "Deploying! " +
                                      "\nScience available is " + currentScienceValue +
                                      "\nRunning experiment: " + experiment.id);

            runningExperiments.Add(experiment.id, (lastUpdate + 10));
            //currentExperiment.DeployExperiment();
            try//taken from ScienceAlert since .DeployExperiment() didnt work and i didnt know about this
            {
              // Get the most-derived type and use its DeployExperiment so we don't
              // skip any plugin-derived versions
              currentExperiment.GetType().InvokeMember("DeployExperiment", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.IgnoreReturn | System.Reflection.BindingFlags.InvokeMethod, null, currentExperiment, null);
            }
            catch (Exception e)
            {
              Debug.LogError("Failed to invoke \"DeployExperiment\" using GetType(), falling back to base type after encountering exception " + e);
              currentExperiment.DeployExperiment();
            }
            if (shipCotainsExperiments.ContainsKey(currentScienceSubject.id))
              shipCotainsExperiments[currentScienceSubject.id]++;
            else
              shipCotainsExperiments.Add(currentScienceSubject.id, 1);
            IsDataToCollect = true;
          }
        }
        else if (currentExperiment.Deployed && currentSettings.getBool("transferScience"))
        {
          IsDataToCollect = true;
        }
      }
    }

    private void checkDataInContainer(ScienceSubject ScienceSubject, ScienceData[] ScienceData)
    {
      if (dataIsInContainer)
        return;
      foreach (ScienceData data in ScienceData)
      {
        if (ScienceSubject.id.Contains(data.subjectID))
        {
          dataIsInContainer = true;
          break;
        }
      }
    }

    private void UpdateCurrent()
    {
      currentVessel = FlightGlobals.ActiveVessel;
      currentBody = currentVessel.mainBody;
      currentSituation = ScienceUtil.GetExperimentSituation(currentVessel);
      if (currentVessel.landedAt != string.Empty)
      {
        currentBiome = currentVessel.landedAt;
      }
      else
        currentBiome = ScienceUtil.GetExperimentBiome(currentBody, currentVessel.latitude, currentVessel.longitude);
      if (shipCotainsExperiments != null)
        shipCotainsExperiments.Clear();
      if (toolbarStrings != null)
        toolbarStrings.Clear();
      if (containerList != null)
        containerList.Clear();
      experimentList = currentVessel.FindPartModulesImplementing<ModuleScienceExperiment>();
      containerList = currentVessel.FindPartModulesImplementing<ModuleScienceContainer>();
      foreach (ModuleScienceContainer currentContainer in containerList)
      {
        addToExpermientedList(currentContainer.GetData());
        toolbarStrings.Add(currentContainer.part.partInfo.title);
      }
      if (container == null && containerList != null)
        container = containerList[0];
    }

    private void addToExpermientedList(ScienceData[] data)
    {
      foreach (ScienceData currentData in data)
      {
        if (shipCotainsExperiments.ContainsKey(currentData.subjectID))
        {
          shipCotainsExperiments[currentData.subjectID] = shipCotainsExperiments[currentData.subjectID] + 1;
        }
        else
        {
          shipCotainsExperiments.Add(currentData.subjectID, 1);
        }
      }
    }
  }
}