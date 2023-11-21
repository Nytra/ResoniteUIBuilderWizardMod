using System.Collections.Generic;
using ResoniteModLoader;
using FrooxEngine;
using FrooxEngine.UIX;
using Elements.Core;
using Elements.Assets;
using System;

namespace UIBuilderWizardMod
{
	public class UIBuilderWizardMod : ResoniteMod
	{
		public override string Name => "UIBuilderWizardMod";
		public override string Author => "Nytra";
		public override string Version => "1.0.0";
		public override string Link => "https://github.com/Nytra/ResoniteUIBuilderWizardMod";

		const string WIZARD_TITLE = "UI Builder Wizard (Mod)";

		public override void OnEngineInit()
		{
			Engine.Current.RunPostInit(AddMenuOption);
		}
		void AddMenuOption()
		{
			DevCreateNewForm.AddAction("Editor", WIZARD_TITLE, (x) => UIBuilderWizard.CreateWizard(x));
		}

		class UIBuilderWizard
		{
			public static UIBuilderWizard CreateWizard(Slot x)
			{
				return new UIBuilderWizard(x);
			}

			Slot WizardSlot;
			Slot WizardContentSlot;
			Slot WizardDataSlot;
			UIBuilder WizardUI;

			UIBuilder currentBuilder;

			// ===== initial screen =====

			ValueField<string> panelName;
			ValueField<float2> panelSize;
			Button createPanelButton;

			// ===== main screen =====

			// texts
			Text rootText;
			Text currentText;

			// layout
			Button verticalLayoutButton;
			Button horizontalLayoutButton;

			// interaction
			Button buttonButton;
			Button checkboxButton;
			Button refEditorButton;
			Button memberEditorButton;
			ReferenceField<ISyncMember> memberEditorMember;

			// nesting
			Button nestButton;
			Button nestOutButton;
			ReferenceField<Slot> nestIntoSlot;
			Button nestIntoButton;

			UIBuilderWizard(Slot x)
			{
				WizardSlot = x;
				WizardSlot.Tag = "Developer";
				WizardSlot.PersistentSelf = false;
				WizardSlot.LocalScale *= 0.0008f;

				WizardDataSlot = WizardSlot.AddSlot("Data");

				WizardUI = RadiantUI_Panel.SetupPanel(WizardSlot, WIZARD_TITLE.AsLocaleKey(), new float2(800f, 800f));
				RadiantUI_Constants.SetupEditorStyle(WizardUI);

				WizardUI.Canvas.MarkDeveloper();
				WizardUI.Canvas.AcceptPhysicalTouch.Value = false;

				WizardUI.Style.MinHeight = 24f;
				WizardUI.Style.PreferredHeight = 24f;
				WizardUI.Style.PreferredWidth = 400f;
				WizardUI.Style.MinWidth = 400f;

				WizardSlot.PositionInFrontOfUser(float3.Backward, distance: 1f);

				WizardContentSlot = WizardUI.Root;

				RegenerateWizardUI();
			}

			void RegenerateWizardUI()
			{
				//WizardDataSlot.DestroyChildren();
				WizardContentSlot.DestroyChildren();
				WizardUI.ForceNext = WizardContentSlot.GetComponent<RectTransform>();

				if (!ValidateCurrentBuilder())
				{
					// build initial screen

					panelName = WizardDataSlot.FindChildOrAdd("Panel Name").AttachComponent<ValueField<string>>();
					panelName.Value.Value = "Test UIX Panel";
					panelSize = WizardDataSlot.FindChildOrAdd("Panel Size").AttachComponent<ValueField<float2>>();
					panelSize.Value.Value = new float2(800f, 800f);

					VerticalLayout verticalLayout = WizardUI.VerticalLayout(4f, childAlignment: Alignment.TopCenter);
					verticalLayout.ForceExpandHeight.Value = false;

					SyncMemberEditorBuilder.Build(panelName.Value, "Panel Name", null, WizardUI);
					SyncMemberEditorBuilder.Build(panelSize.Value, "Panel Size", null, WizardUI);

					WizardUI.Spacer(24f);

					createPanelButton = WizardUI.Button("Create Panel");
					createPanelButton.LocalPressed += CreatePanel;
				}
				else
				{
					// build main screen

					VerticalLayout verticalLayout = WizardUI.VerticalLayout(4f, childAlignment: Alignment.TopCenter);
					verticalLayout.ForceExpandHeight.Value = false;

					rootText = WizardUI.Text("");
					currentText = WizardUI.Text("");

					WizardUI.Spacer(24f);

					WizardUI.Text("Layout");

					verticalLayoutButton = WizardUI.Button("Vertical Layout");
					verticalLayoutButton.LocalPressed += AddVerticalLayout;

					horizontalLayoutButton = WizardUI.Button("Horizontal Layout");
					horizontalLayoutButton.LocalPressed += AddHorizontalLayout;

					WizardUI.Spacer(24f);

					WizardUI.Text("Interaction");

					buttonButton = WizardUI.Button("Button");
					buttonButton.LocalPressed += AddButton;

					checkboxButton = WizardUI.Button("Checkbox");
					checkboxButton.LocalPressed += AddCheckbox;

					//refEditorSyncRef = WizardDataSlot.FindChildOrAdd("RefEditor ISyncRef").AttachComponent<ReferenceField<ISyncRef>>();

					//WizardUI.Text("RefEditor ISyncRef:").HorizontalAlign.Value = TextHorizontalAlignment.Left;
					//WizardUI.Next("RefEditor ISyncRef");
					//WizardUI.Current.AttachComponent<RefEditor>().Setup(refEditorSyncRef.Reference);

					refEditorButton = WizardUI.Button("RefEditor");
					refEditorButton.LocalPressed += AddRefEditor;

					memberEditorMember = WizardDataSlot.FindChildOrAdd("MemberEditor Member").AttachComponent<ReferenceField<ISyncMember>>();

					WizardUI.Text("SyncMember:").HorizontalAlign.Value = TextHorizontalAlignment.Left;
					WizardUI.Next("MemberEditor Member");
					WizardUI.Current.AttachComponent<RefEditor>().Setup(memberEditorMember.Reference);

					memberEditorButton = WizardUI.Button("MemberEditor");
					memberEditorButton.LocalPressed += AddMemberEditor;

					WizardUI.Spacer(24f);

					WizardUI.Text("Nesting");

					nestButton = WizardUI.Button("Nest");
					nestButton.LocalPressed += Nest;

					nestOutButton = WizardUI.Button("Nest Out");
					nestOutButton.LocalPressed += NestOut;

					nestIntoSlot = WizardDataSlot.FindChildOrAdd("Nest Into Slot").AttachComponent<ReferenceField<Slot>>();

					WizardUI.Text("Slot:").HorizontalAlign.Value = TextHorizontalAlignment.Left;
					WizardUI.Next("Nest Into Slot");
					WizardUI.Current.AttachComponent<RefEditor>().Setup(nestIntoSlot.Reference);

					nestIntoButton = WizardUI.Button("Nest Into");
					nestIntoButton.LocalPressed += NestInto;

					UpdateTexts();
				}
			}

			void UpdateTexts()
			{
				rootText.Content.Value = "Root: " + (currentBuilder.Root?.Name ?? "Null");
				currentText.Content.Value = "Current: " + (currentBuilder.Current?.Name ?? "Null");
			}

			bool IsSlotValid(Slot s)
			{
				if (s == null ||
					s.IsDestroyed ||
					s.IsRemoved ||
					s.IsDisposed)
				{
					return false;
				}
				return true;
			}

			bool ValidateCurrentBuilder()
			{
				if (currentBuilder == null || 
					currentBuilder.Canvas == null ||
					!IsSlotValid(currentBuilder.Canvas.Slot) ||
					(!IsSlotValid(currentBuilder.Root) && !IsSlotValid(currentBuilder.Current)))
				{
					return false;
				}
				return true;
			}

			void CreatePanel(IButton button, ButtonEventData eventData)
			{
				Slot root = WizardSlot.LocalUserSpace.AddSlot(panelName.Value.Value);
				currentBuilder = RadiantUI_Panel.SetupPanel(root, root.Name.AsLocaleKey(), panelSize.Value);
				RadiantUI_Constants.SetupEditorStyle(currentBuilder);
				root.LocalScale *= 0.0005f;
				root.PositionInFrontOfUser(float3.Backward, distance: 1f);
				RegenerateWizardUI();
			}

			void AddButton(IButton button, ButtonEventData eventData)
			{
				WizardAction(button, eventData, () => { currentBuilder.Button("Test Button"); });
			}

			void AddCheckbox(IButton button, ButtonEventData eventData)
			{
				WizardAction(button, eventData, () => { currentBuilder.Checkbox(); });
			}

			void AddRefEditor(IButton button, ButtonEventData eventData)
			{
				WizardAction(button, eventData, () => 
				{
					currentBuilder.Next("RefEditor");
					currentBuilder.Current.AttachComponent<RefEditor>().Setup(null);
				});
			}

			void AddVerticalLayout(IButton button, ButtonEventData eventData)
			{
				WizardAction(button, eventData, () => { currentBuilder.VerticalLayout(4f, childAlignment: Alignment.TopCenter); });
			}

			void AddHorizontalLayout(IButton button, ButtonEventData eventData)
			{
				WizardAction(button, eventData, () => { currentBuilder.HorizontalLayout(4f, childAlignment: Alignment.TopCenter); });
			}

			void AddMemberEditor(IButton button, ButtonEventData eventData)
			{
				WizardAction(button, eventData, () =>
				{
					if (memberEditorMember.Reference.Target != null)
					{
						currentBuilder.Next("MemberEditor");
						SyncMemberEditorBuilder.Build(memberEditorMember.Reference.Target, memberEditorMember.Reference.Target.Name, null, currentBuilder);
					}
				});
			}

			void NestOut(IButton button, ButtonEventData eventData)
			{
				WizardAction(button, eventData, () => { currentBuilder.NestOut(); });
			}

			void Nest(IButton button, ButtonEventData eventData)
			{
				WizardAction(button, eventData, () => { currentBuilder.Nest(); });
			}

			void NestInto(IButton button, ButtonEventData eventData)
			{
				WizardAction(button, eventData, () => 
				{ 
					if (nestIntoSlot.Reference.Target != null)
					{
						currentBuilder.NestInto(nestIntoSlot.Reference.Target);
					}
				});
			}

			void WizardAction(IButton button, ButtonEventData eventData, Action action)
			{
				if (!ValidateCurrentBuilder())
				{
					RegenerateWizardUI();
					return;
				}
				action();
				UpdateTexts();
			}
		}
	}
}