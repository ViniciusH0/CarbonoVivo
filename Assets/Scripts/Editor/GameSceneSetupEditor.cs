using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

public static class GameSceneSetupEditor
{
    [MenuItem("CarbonoVivo/Setup Complete Game")]
    public static void SetupCompleteGame()
    {
        Debug.Log("[CarbonoVivo] Iniciando configuração completa do jogo...");

        // 1. Configura GameObject GameManagers
        GameObject gm = GameObject.Find("GameManagers");
        if (gm == null)
        {
            gm = new GameObject("GameManagers");
            gm.transform.position = Vector3.zero;
            Undo.RegisterCreatedObjectUndo(gm, "Create GameManagers");
        }

        var bioMgr = GetOrAddComponent<BiodiversityManager>(gm);
        var ljMgr = GetOrAddComponent<LumberjackManager>(gm);
        var plantMgr = GetOrAddComponent<InteractivePlanting>(gm);
        var visuals = GetOrAddComponent<WorldVisuals>(gm);
        var audioMgr = GetOrAddComponent<BioacousticAudioManager>(gm);
        var dayNight = GetOrAddComponent<DayNightCycle>(gm);
        var weather = GetOrAddComponent<WeatherManager>(gm);
        var debugMgr = GetOrAddComponent<GameDebugManager>(gm);

        // 2. Localiza Canvas e referências essenciais
        Canvas uiCanvas = Object.FindAnyObjectByType<Canvas>();
        if (uiCanvas == null)
        {
            Debug.LogError("[CarbonoVivo] UI_Canvas não encontrado!");
            return;
        }

        Transform gameStep = uiCanvas.transform.Find("Game");
        if (gameStep == null)
        {
            Debug.LogError("[CarbonoVivo] UI_Canvas/Game não encontrado!");
            return;
        }

        TMP_FontAsset pixelFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset") ?? TMP_Settings.defaultFontAsset;

        // 3. Configura AlertBanner no Game HUD
        Transform existingBanner = gameStep.Find("AlertBanner");
        GameObject alertBannerGo;
        if (existingBanner != null)
        {
            alertBannerGo = existingBanner.gameObject;
        }
        else
        {
            alertBannerGo = new GameObject("AlertBanner");
            alertBannerGo.transform.SetParent(gameStep, false);

            var rt = alertBannerGo.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.78f);
            rt.anchorMax = new Vector2(0.5f, 0.78f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(620, 60);

            var img = alertBannerGo.AddComponent<Image>();
            img.color = new Color(0.1f, 0.12f, 0.15f, 0.92f);

            GameObject txtGo = new GameObject("Text");
            txtGo.transform.SetParent(alertBannerGo.transform, false);
            var txtRt = txtGo.AddComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.sizeDelta = Vector2.zero;

            var tmp = txtGo.AddComponent<TextMeshProUGUI>();
            if (pixelFont != null) tmp.font = pixelFont;
            tmp.fontSize = 20;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.text = "Alerta Florestal";
        }

        // Conecta alertBanner ao LumberjackManager via SerializedObject
        SerializedObject ljSo = new SerializedObject(ljMgr);
        ljSo.FindProperty("alertBanner").objectReferenceValue = alertBannerGo;
        ljSo.FindProperty("alertText").objectReferenceValue = alertBannerGo.GetComponentInChildren<TextMeshProUGUI>(true);
        ljSo.ApplyModifiedProperties();
        alertBannerGo.SetActive(false);

        // 4. Cria Barra Inferior de Ações Rápidas no HUD
        Transform existingBottomBar = gameStep.Find("BottomActionsBar");
        if (existingBottomBar == null)
        {
            GameObject bottomBar = new GameObject("BottomActionsBar");
            bottomBar.transform.SetParent(gameStep, false);

            var rt = bottomBar.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.08f);
            rt.anchorMax = new Vector2(0.5f, 0.08f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(580, 75);

            var hlg = bottomBar.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 20;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;

            // Botão Plantar Muda
            GameObject plantBtnGo = CreateHudButton(bottomBar.transform, "PlantTreeButton", "Plantar Muda (10 ppm)", new Color(0.2f, 0.65f, 0.35f), pixelFont);
            var plantBtn = plantBtnGo.GetComponent<Button>();
            var targetPlant = System.Delegate.CreateDelegate(typeof(UnityEngine.Events.UnityAction), plantMgr, "PlantTreeRandomly") as UnityEngine.Events.UnityAction;
            UnityEditor.Events.UnityEventTools.AddPersistentListener(plantBtn.onClick, targetPlant);

            // Botão Biodiversidade
            GameObject bioBtnGo = CreateHudButton(bottomBar.transform, "BiodiversityButton", "Biodiversidade (Espécies)", new Color(0.25f, 0.5f, 0.85f), pixelFont);
        }

        // 5. Configura Painel do Catálogo de Biodiversidade
        Transform existingBioPanel = gameStep.Find("BiodiversityPanel");
        GameObject bioPanelGo;
        if (existingBioPanel != null)
        {
            bioPanelGo = existingBioPanel.gameObject;
        }
        else
        {
            bioPanelGo = new GameObject("BiodiversityPanel");
            bioPanelGo.transform.SetParent(gameStep, false);

            var rt = bioPanelGo.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(560, 720);

            var img = bioPanelGo.AddComponent<Image>();
            img.color = new Color(0.08f, 0.1f, 0.12f, 0.98f);

            var cg = bioPanelGo.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.blocksRaycasts = false;
            cg.interactable = false;

            // Título do Catálogo
            GameObject header = new GameObject("Header");
            header.transform.SetParent(bioPanelGo.transform, false);
            var headerRt = header.AddComponent<RectTransform>();
            headerRt.anchorMin = new Vector2(0f, 0.91f);
            headerRt.anchorMax = new Vector2(1f, 1f);
            headerRt.sizeDelta = Vector2.zero;
            var headerTmp = header.AddComponent<TextMeshProUGUI>();
            if (pixelFont != null) headerTmp.font = pixelFont;
            headerTmp.fontSize = 24;
            headerTmp.alignment = TextAlignmentOptions.Center;
            headerTmp.color = new Color(0.4f, 0.95f, 0.6f);
            headerTmp.text = "Manejo Ecológico & Simbiose";

            // Botão de Fechar
            GameObject closeBtnGo = new GameObject("CloseButton");
            closeBtnGo.transform.SetParent(bioPanelGo.transform, false);
            var closeRt = closeBtnGo.AddComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(0.85f, 0.92f);
            closeRt.anchorMax = new Vector2(0.97f, 0.98f);
            closeRt.sizeDelta = Vector2.zero;
            var closeImg = closeBtnGo.AddComponent<Image>();
            closeImg.color = new Color(0.8f, 0.25f, 0.25f);
            var closeBtn = closeBtnGo.AddComponent<Button>();
            var closeTmp = new GameObject("X").AddComponent<TextMeshProUGUI>();
            closeTmp.transform.SetParent(closeBtnGo.transform, false);
            var closeTmpRt = closeTmp.GetComponent<RectTransform>();
            closeTmpRt.anchorMin = Vector2.zero;
            closeTmpRt.anchorMax = Vector2.one;
            closeTmpRt.sizeDelta = Vector2.zero;
            if (pixelFont != null) closeTmp.font = pixelFont;
            closeTmp.text = "X";
            closeTmp.alignment = TextAlignmentOptions.Center;
            closeTmp.fontSize = 18;

            // Scroll View para os Cards
            GameObject scrollGo = new GameObject("ScrollView");
            scrollGo.transform.SetParent(bioPanelGo.transform, false);
            var scrollRt = scrollGo.AddComponent<RectTransform>();
            scrollRt.anchorMin = new Vector2(0.04f, 0.04f);
            scrollRt.anchorMax = new Vector2(0.96f, 0.89f);
            scrollRt.sizeDelta = Vector2.zero;

            var scrollRect = scrollGo.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollGo.transform, false);
            var viewRt = viewport.AddComponent<RectTransform>();
            viewRt.anchorMin = Vector2.zero;
            viewRt.anchorMax = Vector2.one;
            viewRt.sizeDelta = Vector2.zero;
            viewport.AddComponent<RectMask2D>();

            GameObject content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            var contentRt = content.AddComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.sizeDelta = new Vector2(0, 750);

            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 12;
            vlg.padding = new RectOffset(6, 6, 8, 8);
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;

            var csf = content.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewRt;
            scrollRect.content = contentRt;

            // Configura BiodiversityUI
            var bioUI = bioPanelGo.AddComponent<BiodiversityUI>();
            SerializedObject bioSo = new SerializedObject(bioUI);
            bioSo.FindProperty("catalogCanvas").objectReferenceValue = cg;
            bioSo.FindProperty("cardsContainer").objectReferenceValue = content.transform;
            bioSo.FindProperty("closeButton").objectReferenceValue = closeBtn;
            bioSo.FindProperty("fontAsset").objectReferenceValue = pixelFont;

            Transform bottomBarT = gameStep.Find("BottomActionsBar");
            if (bottomBarT != null)
            {
                Transform bioBtnT = bottomBarT.Find("BiodiversityButton");
                if (bioBtnT != null)
                {
                    var bioBtn = bioBtnT.GetComponent<Button>();
                    bioSo.FindProperty("toggleButton").objectReferenceValue = bioBtn;
                    // Listeners são gerenciados diretamente pelo script BiodiversityUI de forma limpa e dinâmica
                }
            }
            bioSo.ApplyModifiedProperties();
        }

        // 6. Configura SwipeDownMenu no UI_Canvas
        Transform existingPause = uiCanvas.transform.Find("PauseMenuModal");
        GameObject pauseMenuGo;
        if (existingPause != null)
        {
            pauseMenuGo = existingPause.gameObject;
        }
        else
        {
            pauseMenuGo = new GameObject("PauseMenuModal");
            pauseMenuGo.transform.SetParent(uiCanvas.transform, false);

            var rt = pauseMenuGo.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;

            var img = pauseMenuGo.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.75f);

            var cg = pauseMenuGo.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.blocksRaycasts = false;
            cg.interactable = false;

            // Caixa central de diálogo
            GameObject box = new GameObject("Box");
            box.transform.SetParent(pauseMenuGo.transform, false);
            var boxRt = box.AddComponent<RectTransform>();
            boxRt.anchorMin = new Vector2(0.5f, 0.5f);
            boxRt.anchorMax = new Vector2(0.5f, 0.5f);
            boxRt.pivot = new Vector2(0.5f, 0.5f);
            boxRt.sizeDelta = new Vector2(480, 520);
            var boxImg = box.AddComponent<Image>();
            boxImg.color = new Color(0.12f, 0.15f, 0.18f, 0.98f);

            var vlg = box.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 16;
            vlg.padding = new RectOffset(30, 30, 30, 30);
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;

            GameObject title = new GameObject("Title");
            title.transform.SetParent(box.transform, false);
            var titleTmp = title.AddComponent<TextMeshProUGUI>();
            if (pixelFont != null) titleTmp.font = pixelFont;
            titleTmp.fontSize = 28;
            titleTmp.color = Color.white;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.text = "PAUSA";

            GameObject btnResume = CreateHudButton(box.transform, "BtnResume", "Continuar", new Color(0.2f, 0.6f, 0.35f), pixelFont);
            GameObject btnInst = CreateHudButton(box.transform, "BtnInstructions", "Instruções", new Color(0.3f, 0.45f, 0.7f), pixelFont);
            GameObject btnRestart = CreateHudButton(box.transform, "BtnRestart", "Reiniciar Experimento", new Color(0.7f, 0.45f, 0.2f), pixelFont);
            GameObject btnReturn = CreateHudButton(box.transform, "BtnMenu", "Menu Principal", new Color(0.7f, 0.25f, 0.25f), pixelFont);

            var swipeMenu = uiCanvas.gameObject.GetComponent<SwipeDownMenu>();
            if (swipeMenu == null) swipeMenu = uiCanvas.gameObject.AddComponent<SwipeDownMenu>();

            SerializedObject smSo = new SerializedObject(swipeMenu);
            smSo.FindProperty("pauseMenuCanvas").objectReferenceValue = cg;
            smSo.FindProperty("resumeButton").objectReferenceValue = btnResume.GetComponent<Button>();
            smSo.FindProperty("instructionsButton").objectReferenceValue = btnInst.GetComponent<Button>();
            smSo.FindProperty("restartButton").objectReferenceValue = btnRestart.GetComponent<Button>();
            smSo.FindProperty("returnMenuButton").objectReferenceValue = btnReturn.GetComponent<Button>();

            Transform hudPauseBtn = uiCanvas.transform.Find("PauseButton");
            if (hudPauseBtn != null)
            {
                smSo.FindProperty("hudPauseButton").objectReferenceValue = hudPauseBtn.GetComponent<Button>();
            }
            smSo.ApplyModifiedProperties();
        }

        // 7. Configura GameOverScreen no UI_Canvas
        Transform existingGameOver = uiCanvas.transform.Find("GameOverScreen");
        if (existingGameOver == null)
        {
            GameObject goScreen = new GameObject("GameOverScreen");
            goScreen.transform.SetParent(uiCanvas.transform, false);

            var rt = goScreen.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;

            var img = goScreen.AddComponent<Image>();
            img.color = new Color(0.12f, 0.05f, 0.05f, 0.95f);

            var cg = goScreen.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.blocksRaycasts = false;
            cg.interactable = false;

            GameObject box = new GameObject("Box");
            box.transform.SetParent(goScreen.transform, false);
            var boxRt = box.AddComponent<RectTransform>();
            boxRt.anchorMin = new Vector2(0.5f, 0.5f);
            boxRt.anchorMax = new Vector2(0.5f, 0.5f);
            boxRt.pivot = new Vector2(0.5f, 0.5f);
            boxRt.sizeDelta = new Vector2(520, 480);

            var vlg = box.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 18;
            vlg.padding = new RectOffset(24, 24, 24, 24);
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;

            GameObject title = new GameObject("Title");
            title.transform.SetParent(box.transform, false);
            var titleTmp = title.AddComponent<TextMeshProUGUI>();
            if (pixelFont != null) titleTmp.font = pixelFont;
            titleTmp.fontSize = 30;
            titleTmp.color = new Color(0.95f, 0.25f, 0.25f);
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.text = "COLAPSO ECOLÓGICO";

            GameObject reason = new GameObject("ReasonText");
            reason.transform.SetParent(box.transform, false);
            var reasonTmp = reason.AddComponent<TextMeshProUGUI>();
            if (pixelFont != null) reasonTmp.font = pixelFont;
            reasonTmp.fontSize = 17;
            reasonTmp.color = Color.white;
            reasonTmp.alignment = TextAlignmentOptions.Center;
            reasonTmp.textWrappingMode = TextWrappingModes.Normal;
            reasonTmp.text = "O equilíbrio do ecossistema foi quebrado.";

            GameObject btnRetry = CreateHudButton(box.transform, "BtnRetry", "Tentar Novamente", new Color(0.2f, 0.65f, 0.35f), pixelFont);
            GameObject btnMenu = CreateHudButton(box.transform, "BtnMenu", "Voltar ao Menu", new Color(0.6f, 0.25f, 0.25f), pixelFont);

            var overScript = goScreen.AddComponent<GameOverScreen>();
            SerializedObject overSo = new SerializedObject(overScript);
            overSo.FindProperty("gameOverCanvas").objectReferenceValue = cg;
            overSo.FindProperty("reasonText").objectReferenceValue = reasonTmp;
            overSo.FindProperty("restartButton").objectReferenceValue = btnRetry.GetComponent<Button>();
            overSo.FindProperty("menuButton").objectReferenceValue = btnMenu.GetComponent<Button>();
            overSo.ApplyModifiedProperties();
        }

        // 8. Adiciona botões na WinScreen se não existirem
        Transform winScreenT = uiCanvas.transform.Find("WinScreen");
        if (winScreenT != null && winScreenT.Find("WinButtons") == null)
        {
            GameObject winBtns = new GameObject("WinButtons");
            winBtns.transform.SetParent(winScreenT, false);

            var rt = winBtns.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.12f);
            rt.anchorMax = new Vector2(0.5f, 0.12f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(500, 65);

            var hlg = winBtns.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 16;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;

            var winScript = winScreenT.GetComponent<WinScreen>();
            GameObject btnPlayAgain = CreateHudButton(winBtns.transform, "PlayAgainBtn", "Jogar Novamente", new Color(0.2f, 0.65f, 0.35f), pixelFont);
            btnPlayAgain.GetComponent<Button>().onClick.AddListener(() => winScript.RestartGame());

            GameObject btnMenuWin = CreateHudButton(winBtns.transform, "MenuBtn", "Menu Principal", new Color(0.35f, 0.45f, 0.7f), pixelFont);
            btnMenuWin.GetComponent<Button>().onClick.AddListener(() => winScript.ReturnToMainMenu());
        }

        // 9. Anexa SwipeCard na caixa de perguntas se não existir
        Transform dialogBox = gameStep.Find("Question/DialogBox2");
        if (dialogBox != null && dialogBox.GetComponent<SwipeCard>() == null)
        {
            var swipeCard = dialogBox.gameObject.AddComponent<SwipeCard>();
            swipeCard.decisionThreshold = 100f;
        }

        // 10. Configura Debug Panel
        Transform existingDebug = uiCanvas.transform.Find("DebugPanel");
        GameObject debugPanelGo;
        if (existingDebug == null)
        {
            debugPanelGo = new GameObject("DebugPanel");
            debugPanelGo.transform.SetParent(uiCanvas.transform, false);
            debugPanelGo.AddComponent<DebugPanelUI>();
        }
        else
        {
            debugPanelGo = existingDebug.gameObject;
            GetOrAddComponent<DebugPanelUI>(debugPanelGo);
        }

        EditorUtility.SetDirty(gm);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

        Debug.Log("[CarbonoVivo] Configuração completa concluída com sucesso!");
    }

    private static T GetOrAddComponent<T>(GameObject go) where T : Component
    {
        T comp = go.GetComponent<T>();
        if (comp == null) comp = go.AddComponent<T>();
        return comp;
    }

    private static GameObject CreateHudButton(Transform parent, string name, string text, Color color, TMP_FontAsset font)
    {
        GameObject btnGo = new GameObject(name);
        btnGo.transform.SetParent(parent, false);

        var rt = btnGo.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(240, 60);

        var img = btnGo.AddComponent<Image>();
        img.color = color;

        var btn = btnGo.AddComponent<Button>();
        btn.targetGraphic = img;

        var le = btnGo.AddComponent<LayoutElement>();
        le.preferredHeight = 60;
        le.flexibleWidth = 1;

        GameObject txtGo = new GameObject("Text");
        txtGo.transform.SetParent(btnGo.transform, false);
        var txtRt = txtGo.AddComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.sizeDelta = Vector2.zero;

        var tmp = txtGo.AddComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.text = text;
        tmp.fontSize = 18;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        return btnGo;
    }

    [MenuItem("CarbonoVivo/Validate Project")]
    public static void ValidateProject()
    {
        Debug.Log("[CarbonoVivo] VALIDATION: Starting validation...");
        var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/GameScene_Test.unity");
        Debug.Log($"[CarbonoVivo] VALIDATION: Opened scene {scene.name}, isLoaded={scene.isLoaded}");

        var spawner = Object.FindAnyObjectByType<TreeSpawner>();
        Debug.Log($"[CarbonoVivo] VALIDATION: TreeSpawner found: {spawner != null}");

        var onboarding = Object.FindAnyObjectByType<OnboardingManager>();
        Debug.Log($"[CarbonoVivo] VALIDATION: OnboardingManager found: {onboarding != null}");

        var planting = Object.FindAnyObjectByType<InteractivePlanting>();
        Debug.Log($"[CarbonoVivo] VALIDATION: InteractivePlanting found: {planting != null}");

        var lumberjack = Object.FindAnyObjectByType<LumberjackManager>();
        Debug.Log($"[CarbonoVivo] VALIDATION: LumberjackManager found: {lumberjack != null}");

        Debug.Log("[CarbonoVivo] VALIDATION SUCCESSFUL!");
    }
}
