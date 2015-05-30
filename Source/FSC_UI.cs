using KerboKatz.Extensions;
using UnityEngine;

namespace KerboKatz
{
  partial class ForScienceContinued : KerboKatzBase
  {
    private GUIStyle buttonStyle;
    private GUIStyle containerStyle;
    private GUIStyle numberFieldStyle;
    private GUIStyle windowStyle, labelStyle, toggleStyle, textStyle;
    private Rectangle windowPosition = new Rectangle(Rectangle.updateType.Cursor);
    private float lastWindowHeight;
    private bool doEVAonlyIfOnGroundWhenLanded;
    private bool dumpDuplicateResults;
    private bool resetExperiments;
    private bool initStyle = false;
    private string scienceCutoff;
    private string spriteAnimationFPS;
    private bool windowShrinked;
    private bool runOneTimeScience;
    private bool setWindowShrinked;
    private bool transferScience;
    private int toolbarInt;
    private static int windowID = Utilities.UI.getNewWindowID;
    private bool transferAll;
    private bool makeScienceForDMagic;
    private bool hideScienceReports;

    private void OnGUI()
    {
      if (!initStyle)
        InitStyle();
      Utilities.UI.createWindow(currentSettings.getBool("showSettings"), windowID, ref windowPosition, MainWindow, "For Science", windowStyle);
      if (currentSettings.getBool("showSettings"))
      {
        if (Event.current.type == EventType.Repaint)
        {
          if (windowPosition.height != 0 && windowPosition.height != lastWindowHeight || !windowShrinked)
          {
            windowPosition.height = 0;
            windowShrinked = true;
          }
          if (windowPosition.height != 0)
          {
            lastWindowHeight = windowPosition.height;
          }
        }
      }
      Utilities.UI.showTooltip();
    }

    private void InitStyle()
    {
      labelStyle = new GUIStyle(HighLogic.Skin.label);
      labelStyle.stretchWidth = true;

      windowStyle = new GUIStyle(HighLogic.Skin.window);
      windowStyle.fixedWidth = 250;
      windowStyle.padding.left = 0;

      toggleStyle = new GUIStyle(HighLogic.Skin.toggle);
      toggleStyle.normal.textColor = labelStyle.normal.textColor;
      toggleStyle.active.textColor = labelStyle.normal.textColor;

      textStyle = new GUIStyle(HighLogic.Skin.label);
      textStyle.fixedWidth = 100;
      textStyle.margin.left = 10;

      containerStyle = new GUIStyle(GUI.skin.button);
      containerStyle.fixedWidth = 230;
      containerStyle.margin.left = 10;

      numberFieldStyle = new GUIStyle(HighLogic.Skin.box);
      numberFieldStyle.fixedWidth = 52;
      numberFieldStyle.fixedHeight = 22;
      numberFieldStyle.alignment = TextAnchor.MiddleCenter;
      numberFieldStyle.margin.left = 95;
      numberFieldStyle.padding.right = 7;

      buttonStyle = new GUIStyle(GUI.skin.button);
      buttonStyle.fixedWidth = 127;

      initStyle = true;
    }

    private void MainWindow(int windowID)
    {
      GUILayout.BeginVertical();
      GUILayout.BeginHorizontal();
      Utilities.UI.createLabel("Animation FPS:", textStyle, "set to 0 to disable");
      spriteAnimationFPS = Utilities.getOnlyNumbers(GUILayout.TextField(spriteAnimationFPS, 5, numberFieldStyle));
      GUILayout.EndHorizontal();
      GUILayout.BeginHorizontal();
      Utilities.UI.createLabel("Science cutoff:", textStyle, "Doesn't run any science experiment if it's value less than this number.");
      scienceCutoff = Utilities.getOnlyNumbers(GUILayout.TextField(scienceCutoff, 5, numberFieldStyle));
      GUILayout.EndHorizontal();
      if (GUILayout.Toggle(doEVAonlyIfOnGroundWhenLanded, new GUIContent("Restrict EVA-Report", "If this option is turned on and the vessel is landed/splashed the kerbal wont do the EVA-Report if he isnt on the ground."), toggleStyle))
      {
        doEVAonlyIfOnGroundWhenLanded = true;
      }
      else
      {
        doEVAonlyIfOnGroundWhenLanded = false;
      }
      if (GUILayout.Toggle(runOneTimeScience, new GUIContent("Run one-time only science", "To run experiments like goo container"), toggleStyle))
      {
        runOneTimeScience = true;
      }
      else
      {
        runOneTimeScience = false;
      }
      if (GUILayout.Toggle(resetExperiments, new GUIContent("Reset experiments automatically", "This option only works if you have a Scientis on board"), toggleStyle))
      {
        resetExperiments = true;
      }
      else
      {
        resetExperiments = false;
      }
      if (GUILayout.Toggle(hideScienceReports, new GUIContent("Hide experiments result window", "This option hides all the experiment result windows created by it. This might cause some issues. If you notice any unwanted behaviour please report it in the thread"), toggleStyle))
      {
        hideScienceReports = true;
        if (GUILayout.Toggle(makeScienceForDMagic, new GUIContent("Create results for DMagic", "This option creates experiment results for DMagic Orbital Science experiments in order to hide the results window. This might cause some issues. If you notice any unwanted behaviour please report it in the thread"), toggleStyle))
        {
          makeScienceForDMagic = true;
        }
        else
        {
          makeScienceForDMagic = false;
        }
      }
      else
      {
        hideScienceReports = false;
      }
      if (GUILayout.Toggle(transferScience, new GUIContent("Transfer science to container", "Transfers all the science from experiments to the selected container.\nWARNING: makes experiments unoperable if used with \"Run one-time only science\""), toggleStyle))
      {
        transferScience = true;
        GUILayout.BeginVertical();
        if (Utilities.UI.createToggle("Transfer all experiments", transferAll, toggleStyle, "If you turn this on all experiments,including non rerunable, will be transfered to your selected science container."))
        {
          transferAll = true;
        }
        else
        {
          transferAll = false;
        }
        if (Utilities.UI.createToggle("Dump duplicate results", dumpDuplicateResults, toggleStyle, "If you turn this on experiments that are already stored will be dumped over board."))
        {
          dumpDuplicateResults = true;
        }
        else
        {
          dumpDuplicateResults = false;
        }
        if (toolbarStrings != null)
          toolbarInt = GUILayout.SelectionGrid(toolbarInt, toolbarStrings.ToArray(), 1, containerStyle);
        GUILayout.EndVertical();
        setWindowShrinked = false;
      }
      else
      {
        if (!setWindowShrinked)
        {
          windowShrinked = false;
          setWindowShrinked = true;
        }
        transferScience = false;
      }
      Utilities.UI.createOptionSwitcher("Use:", Toolbar.toolbarOptions, ref toolbarSelected);
      GUILayout.BeginHorizontal();
      GUILayout.FlexibleSpace();
      if (GUILayout.Button("Save", buttonStyle))
      {
        updateToolbarBool();
        currentSettings.set("resetExperiments", resetExperiments);
        currentSettings.set("scienceCutoff", scienceCutoff);
        currentSettings.set("spriteAnimationFPS", spriteAnimationFPS);
        currentSettings.set("transferScience", transferScience);
        currentSettings.set("doEVAonlyIfOnGroundWhenLanded", doEVAonlyIfOnGroundWhenLanded);
        currentSettings.set("runOneTimeScience", runOneTimeScience);
        currentSettings.set("transferAll", transferAll);
        currentSettings.set("dumpDuplicateResults", dumpDuplicateResults);
        currentSettings.set("hideScienceReports", hideScienceReports);
        currentSettings.set("makeScienceForDMagic", makeScienceForDMagic);
        currentSettings.save();
        currentSettings.set("showSettings", false);
        if (containerList != null)
          container = containerList[toolbarInt];

        updateFrameCheck();
      }
      GUILayout.FlexibleSpace();
      GUILayout.EndHorizontal();
      GUILayout.EndVertical();
      Utilities.UI.updateTooltipAndDrag();
    }
  }
}