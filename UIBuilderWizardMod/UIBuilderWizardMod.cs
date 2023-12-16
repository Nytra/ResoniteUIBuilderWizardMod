using System.Collections.Generic;
using ResoniteModLoader;
using FrooxEngine;
using FrooxEngine.UIX;
using Elements.Core;
using Elements.Assets;
using System;
using System.Reflection;
using HarmonyLib;
using FrooxEngine.Undo;

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

			private static FieldInfo rootsField = AccessTools.Field(typeof(UIBuilder), "roots");
			private static FieldInfo uiStylesField = AccessTools.Field(typeof(UIBuilder), "_uiStyles");
			private static FieldInfo currentField = AccessTools.Field(typeof(UIBuilder), "Current");

			Slot WizardSlot;
			Slot WizardContentSlot;
			RectTransform WizardContentRect;
			Slot WizardDataSlot;
			UIBuilder WizardUI;
			//UIStyle WizardStyle;
			Slot lastRoot;
			Slot lastCurrent;
			
			UIBuilder currentBuilder;
			//IWorldElement lastElement;

			// ===== initial screen =====

			ValueField<string> panelName;
			ValueField<float2> panelSize;
			Button createPanelButton;

			// ===== main screen =====

			// texts
			Text rootText;
			Text currentText;
			//Text styleText;

			// extra
			Button openRootSlotButton;
			Button openCurrentSlotButton;

			Button editStyleButton;
			//ValueField<bool> openWorkerInspectors;

			// layout
			Button verticalLayoutButton;
			Button horizontalLayoutButton;
			Button scrollAreaButton;
			Button spacerButton;
			Button scrollingVerticalLayoutButton;

			// graphics
			Button imageButton;
			//ValueField<colorX> imageColor;
			Button textButton;

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
				WizardSlot.LocalScale *= 0.0006f;

				WizardDataSlot = WizardSlot.AddSlot("Data");

				WizardUI = RadiantUI_Panel.SetupPanel(WizardSlot, WIZARD_TITLE.AsLocaleKey(), new float2(800f, 1500f));
				RadiantUI_Constants.SetupEditorStyle(WizardUI);

				WizardUI.Canvas.MarkDeveloper();
				WizardUI.Canvas.AcceptPhysicalTouch.Value = false;

				//WizardUI.Style.MinHeight = 24f;
				WizardUI.Style.PreferredHeight = 24f;
				WizardUI.Style.PreferredWidth = 96f;
				//WizardUI.Style.MinWidth = 400f;

				WizardSlot.PositionInFrontOfUser(float3.Backward, distance: 1f);

				WizardContentSlot = WizardUI.Root;
				WizardContentRect = WizardUI.CurrentRect;

				//WizardStyle = WizardUI.Style.Clone();

				RegenerateWizardUI();
			}

			void RegenerateWizardUI()
			{
				WizardDataSlot.DestroyChildren();
				WizardContentSlot.DestroyChildren();
				//var rect = WizardContentSlot.GetComponent<RectTransform>();
				WizardUI.ForceNext = WizardContentRect;
				WizardContentSlot.RemoveAllComponents((Component c) => c != WizardContentRect);

				if (!ValidateCurrentBuilder())
				{
					// build initial screen

					//WizardUI.PushStyle();

					panelName = WizardDataSlot.FindChildOrAdd("Panel Name").GetComponentOrAttach<ValueField<string>>();
					panelName.Value.Value = "Test UIX Panel";
					panelSize = WizardDataSlot.FindChildOrAdd("Panel Size").GetComponentOrAttach<ValueField<float2>>();
					panelSize.Value.Value = new float2(800f, 800f);

					VerticalLayout verticalLayout = WizardUI.VerticalLayout(4f, childAlignment: Alignment.TopCenter);
					verticalLayout.ForceExpandHeight.Value = false;

					SyncMemberEditorBuilder.Build(panelName.Value, "Panel Name", null, WizardUI);
					SyncMemberEditorBuilder.Build(panelSize.Value, "Panel Size", null, WizardUI);

					//GenerateStyleMemberEditors(WizardUI, WizardUI.Style);

					WizardUI.Spacer(24f);

					createPanelButton = WizardUI.Button("Create Panel");
					createPanelButton.LocalPressed += (btn, data) => 
					{
						Slot root = WizardSlot.LocalUserSpace.AddSlot(panelName.Value.Value);
						currentBuilder = CreatePanel(root, root.Name, panelSize.Value.Value);
						currentBuilder.Root.OnPrepareDestroy += (slot) => 
						{
                            // Run an empty action after the slot gets destroyed simply to update the wizard UI
                            WizardSlot.RunSynchronously(() => 
							{ 
								WizardAction(null, new ButtonEventData(), () => { }); 
							});
						};
						CopyStyle(WizardUI, currentBuilder);
						//WizardUI.PopStyle();
						RegenerateWizardUI();
					};
				}
				else
				{
					// build main screen

					//WizardUI.ScrollArea();
					//WizardUI.FitContent(SizeFit.Disabled, SizeFit.PreferredSize);

					VerticalLayout verticalLayout = WizardUI.VerticalLayout(4f, childAlignment: Alignment.TopCenter);
					verticalLayout.ForceExpandHeight.Value = false;

					rootText = WizardUI.Text("");
					currentText = WizardUI.Text("");
					//styleText = WizardUI.Text("");

					openRootSlotButton = WizardUI.Button("Open Root Slot");
					openRootSlotButton.LocalPressed += OpenRootSlot;

					openCurrentSlotButton = WizardUI.Button("Open Current Slot");
					openCurrentSlotButton.LocalPressed += OpenCurrentSlot;

					WizardUI.Spacer(24f);

					editStyleButton = WizardUI.Button("Edit Style");
					editStyleButton.LocalPressed += EditStyle;

					//openWorkerInspectors = WizardDataSlot.FindChildOrAdd("Open Worker Inspectors").GetComponentOrAttach<ValueField<bool>>();
					//WizardUI.HorizontalElementWithLabel("Open Worker Inspectors:", 0.942f, () => WizardUI.BooleanMemberEditor(openWorkerInspectors.Value));

					WizardUI.Spacer(24f);

					WizardUI.Text("Layout");

					verticalLayoutButton = WizardUI.Button("Vertical Layout");
					verticalLayoutButton.LocalPressed += AddVerticalLayout;

					//scrollingVerticalLayoutButton = WizardUI.Button("Scrolling Vertical Layout");
					//scrollingVerticalLayoutButton.LocalPressed += AddScrollingVerticalLayout;

					//Button newVerticalLayoutButton = WizardUI.Button("New Vertical Layout");
					//               newVerticalLayoutButton.LocalPressed += (btn, data) => 
					//{ 
					//	WizardAction(btn, data, () => 
					//	{
					//		MethodInfo method = AccessTools.Method(typeof(UIBuilder), "VerticalLayout", new Type[] { typeof(float), typeof(float), typeof(float), typeof(float), typeof(float), typeof(Alignment?) });
					//		TestPanelWithArgs(method);
					//                   });
					//};

					horizontalLayoutButton = WizardUI.Button("Horizontal Layout");
					horizontalLayoutButton.LocalPressed += AddHorizontalLayout;

					scrollAreaButton = WizardUI.Button("Scroll Area");
					scrollAreaButton.LocalPressed += AddScrollArea;

					spacerButton = WizardUI.Button("Spacer");
					spacerButton.LocalPressed += AddSpacer;

					WizardUI.Spacer(24f);

					WizardUI.Text("Graphics");

					//imageColor = WizardDataSlot.FindChildOrAdd("Image Color").GetComponentOrAttach<ValueField<colorX>>();
					//imageColor.Value.Value = colorX.White;

					//WizardUI.Text("Color:").HorizontalAlign.Value = TextHorizontalAlignment.Left;
					//SyncMemberEditorBuilder.Build(imageColor.Value, imageColor.Value.Name, null, WizardUI);

					imageButton = WizardUI.Button("Image");
					imageButton.LocalPressed += AddImage;

					textButton = WizardUI.Button("Text");
					textButton.LocalPressed += AddText;

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

					memberEditorMember = WizardDataSlot.FindChildOrAdd("MemberEditor Member").GetComponentOrAttach<ReferenceField<ISyncMember>>();

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

					nestIntoSlot = WizardDataSlot.FindChildOrAdd("Nest Into Slot").GetComponentOrAttach<ReferenceField<Slot>>();

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
				var roots = (Stack<Slot>)rootsField.GetValue(currentBuilder);
				var uiStyles = (Stack<UIStyle>)uiStylesField.GetValue(currentBuilder);

				rootText.Content.Value = $"[{roots.Count}] Root: ";
				if (IsSlotValid(currentBuilder.Root))
				{
					rootText.Content.Value += currentBuilder.Root.Name;
				}
				else
				{
					rootText.Content.Value += "Null";
				}
				currentText.Content.Value = "Current: ";
				if (IsSlotValid(currentBuilder.Current))
				{
					currentText.Content.Value += currentBuilder.Current.Name;
				}
				else
				{
					currentText.Content.Value += "Null";
				}
				//styleText.Content.Value = "Styles count: " + uiStyles.Count;
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

			UIBuilder CreatePanel(Slot root, string name, float2 size)
			{
				UIBuilder builder = RadiantUI_Panel.SetupPanel(root, name.AsLocaleKey(), size);
				RadiantUI_Constants.SetupEditorStyle(builder);
				root.LocalScale *= 0.0005f;
				root.PositionInFrontOfUser(float3.Backward, distance: 1f);
				return builder;
			}

			void CopyStyle(UIBuilder b1, UIBuilder b2)
			{
				FieldInfo[] fields = typeof(UIStyle).GetFields();
				int i = 0;
				foreach (FieldInfo field in fields)
				{
					field.SetValue(b2.Style, field.GetValue(b1.Style));
					i++;
				}
			}

			void GenerateStyleMemberEditors(UIBuilder UI, UIStyle targetStyle)
			{
				FieldInfo[] fields = typeof(UIStyle).GetFields();
				int i = 0;
				foreach (FieldInfo field in fields)
				{
					Slot s = WizardDataSlot.FindChildOrAdd(i.ToString() + "_" + field.Name);
					if (field.FieldType.IsValueType)
					{
						Type t = typeof(ValueField<>).MakeGenericType(field.FieldType);
						Component c = s.GetComponent(t);
						bool subscribe = false;
						if (c == null)
						{
							c = s.AttachComponent(t);
							subscribe = true;
						}
						ISyncMember member = c.GetSyncMember("Value");
						((IField)member).BoxedValue = field.GetValue(targetStyle);
						SyncMemberEditorBuilder.Build(member, field.Name, null, UI);
						if (subscribe)
						{
							member.Changed += (value) => { field.SetValue(targetStyle, ((IField)value).BoxedValue); };
						}
					}
					else
					{
						Type t = typeof(ReferenceField<>).MakeGenericType(field.FieldType);
						Component c = s.GetComponent(t);
						bool subscribe = false;
						if (c == null)
						{
							c = s.AttachComponent(t);
							subscribe = true;
						}
						ISyncMember member = c.GetSyncMember("Reference");
						((ISyncRef)member).Target = (IWorldElement)field.GetValue(targetStyle);
						SyncMemberEditorBuilder.Build(member, field.Name, null, UI);
						if (subscribe)
						{
							member.Changed += (value) => { field.SetValue(targetStyle, ((ISyncRef)value).Target); };
						}
					}
					i++;
				}
			}

			void CreatePanelWithMethodParameters(MethodInfo method)
			{
				Slot root = WizardSlot.LocalUserSpace.AddSlot("Test Panel with Args");
				UIBuilder UI = CreatePanel(root, root.Name, new float2(800, 800));

				VerticalLayout verticalLayout = UI.VerticalLayout(4f, childAlignment: Alignment.TopCenter);
				verticalLayout.ForceExpandHeight.Value = false;

				ParameterInfo[] parameters = method.GetParameters();
				int i = 0;
				foreach (ParameterInfo param in parameters) 
				{
					Slot s = WizardDataSlot.FindChildOrAdd(i.ToString() + "_" + param.Name);
					if (param.ParameterType.IsValueType)
					{
						Type t = typeof(ValueField<>).MakeGenericType(param.ParameterType);
						Component c = s.AttachComponent(t);
						SyncMemberEditorBuilder.Build(c.GetSyncMember("Value"), param.Name, null, UI);
					}
					else
					{
						Type t = typeof(ReferenceField<>).MakeGenericType(param.ParameterType);
						Component c = s.AttachComponent(t);
						SyncMemberEditorBuilder.Build(c.GetSyncMember("Reference"), param.Name, null, UI);
					}
					i++;
				}
			}

			// ===== ACTIONS =====

			void OpenRootSlot(IButton button, ButtonEventData eventData)
			{
				WizardAction(button, eventData, () =>
				{
					currentBuilder.Root.OpenInspectorForTarget();
				});
			}

			void OpenCurrentSlot(IButton button, ButtonEventData eventData)
			{
				WizardAction(button, eventData, () =>
				{
					currentBuilder.Current.OpenInspectorForTarget();
				});
			}

			void EditStyle(IButton button, ButtonEventData eventData)
			{
				WizardAction(button, eventData, () =>
				{
					Slot s = WizardSlot.LocalUserSpace.AddSlot("Style Edit Panel");
					UIBuilder b = CreatePanel(s, s.Name, new float2(800, 1500));
					b.Canvas.MarkDeveloper();
					b.Canvas.AcceptPhysicalTouch.Value = false;
					VerticalLayout verticalLayout = b.VerticalLayout(4f, childAlignment: Alignment.TopCenter);
					verticalLayout.ForceExpandHeight.Value = false;
					GenerateStyleMemberEditors(b, currentBuilder.Style);
				});
			}

			void AddVerticalLayout(IButton button, ButtonEventData eventData)
			{
				WizardAction(button, eventData, () => 
				{
					VerticalLayout verticalLayout = currentBuilder.VerticalLayout();
					//lastElement = verticalLayout;
					verticalLayout.OpenInspectorForTarget(openWorkerOnly: true);
				});
			}

			void AddScrollingVerticalLayout(IButton button, ButtonEventData eventData)
			{
				WizardAction(button, eventData, () =>
				{
					currentBuilder.ScrollArea();
					currentBuilder.FitContent(SizeFit.Disabled, SizeFit.PreferredSize);
					currentBuilder.VerticalLayout();
				});
			}

			void AddHorizontalLayout(IButton button, ButtonEventData eventData)
			{
				WizardAction(button, eventData, () => 
				{
					HorizontalLayout horizontalLayout = currentBuilder.HorizontalLayout();
					//lastElement = horizontalLayout;
					horizontalLayout.OpenInspectorForTarget(openWorkerOnly: true);
				});
			}

			void AddScrollArea(IButton button, ButtonEventData eventData)
			{
				WizardAction(button, eventData, () =>
				{
					ScrollRect scrollRect = currentBuilder.ScrollArea();
					//lastElement = scrollRect;
					scrollRect.OpenInspectorForTarget(openWorkerOnly: true);
					ContentSizeFitter fitter = currentBuilder.FitContent();
					//lastElement = fitter;
					fitter.OpenInspectorForTarget(openWorkerOnly: true);
				});
			}

			void AddSpacer(IButton button, ButtonEventData eventData)
			{
				WizardAction(button, eventData, () =>
				{
					RectTransform rect = currentBuilder.Spacer(24f);
					//lastElement = rect;
				});
			}

			void AddImage(IButton button, ButtonEventData eventData)
			{
				WizardAction(button, eventData, () =>
				{
					Image img = currentBuilder.Image();
					img.OpenInspectorForTarget(openWorkerOnly: true);
					//lastElement = img;
				});
			}

			void AddText(IButton button, ButtonEventData eventData)
			{
				WizardAction(button, eventData, () =>
				{
					Text t = currentBuilder.Text("Text");
					t.OpenInspectorForTarget(openWorkerOnly: true);
					//lastElement = t;
				});
			}

			void AddButton(IButton button, ButtonEventData eventData)
			{
				WizardAction(button, eventData, () => 
				{ 
					Button b = currentBuilder.Button("Button");
					//lastElement = b;
				});
			}

			void AddCheckbox(IButton button, ButtonEventData eventData)
			{
				WizardAction(button, eventData, () => 
				{
					Checkbox checkbox = currentBuilder.Checkbox();
					checkbox.OpenInspectorForTarget(openWorkerOnly: true);
					//lastElement = checkbox;
				});
			}

			void AddRefEditor(IButton button, ButtonEventData eventData)
			{
				WizardAction(button, eventData, () => 
				{
					currentBuilder.Next("RefEditor");
					RefEditor refEditor = currentBuilder.Current.AttachComponent<RefEditor>();
					refEditor.Setup(null);
					//lastElement = refEditor;
					refEditor.OpenInspectorForTarget(openWorkerOnly: true);
				});
			}

			void AddMemberEditor(IButton button, ButtonEventData eventData)
			{
				WizardAction(button, eventData, () =>
				{
					if (memberEditorMember.Reference.Target != null)
					{
						SyncMemberEditorBuilder.Build(memberEditorMember.Reference.Target, memberEditorMember.Reference.Target.Name, null, currentBuilder);
					}
				});
			}

			void Nest(IButton button, ButtonEventData eventData)
			{
				WizardAction(button, eventData, () => 
				{ 
					currentBuilder.Nest(); 
				});
			}

			void NestOut(IButton button, ButtonEventData eventData)
			{
				WizardAction(button, eventData, () => 
				{ 
					currentBuilder.NestOut(); 
				});
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
				bool didNestOut = false;

				if (!ValidateCurrentBuilder())
				{
					if (IsSlotValid(currentBuilder?.Canvas?.Slot))
					{
						while (!IsSlotValid(currentBuilder.Current) && !IsSlotValid(currentBuilder.Root))
						{
							currentBuilder.NestOut();
							didNestOut = true;
						}
					}
					else
					{
						RegenerateWizardUI();
						return;
					}
				}

				// Run the action
				action();

                bool flagUndoRoot = false;
                bool flagUndoCurrent = false;

                if (!didNestOut && lastRoot != currentBuilder.Root && IsSlotValid(currentBuilder.Root))
                {
                    flagUndoRoot = true;
                }
                if (lastCurrent != currentBuilder.Current && IsSlotValid(currentBuilder.Current))
                {
                    flagUndoCurrent = true;
                }

                // Subscribe to the OnPrepareDestroy event so that the wizard UI can refresh
                currentBuilder.Root.OnPrepareDestroy += (slot) =>
                {
                    // Run an empty action after the slot gets destroyed simply to update the wizard UI
                    WizardSlot.RunSynchronously(() =>
					{
                        WizardAction(null, new ButtonEventData(), () => { });
                    });
                };

                if (IsSlotValid(currentBuilder.Current))
                {
                    currentBuilder.Current.OnPrepareDestroy += (slot) =>
                    {
                        // Run an empty action after the slot gets destroyed simply to update the wizard UI
                        WizardSlot.RunSynchronously(() =>
                        {
                            WizardAction(null, new ButtonEventData(), () => { });
                        });
                    };
                }

                if (flagUndoRoot)
				{
                    currentBuilder.World.BeginUndoBatch(button.LabelText);
                    var spawnOrDestroy = currentBuilder.Root.CreateSpawnUndoPoint();
                    currentBuilder.World.EndUndoBatch();
					spawnOrDestroy.Target.Changed += (changeable) => 
					{
						var syncRef = changeable as ISyncRef;
                        if (syncRef.Target != null)
						{
							currentBuilder.NestInto((Slot)syncRef.Target);
							UpdateTexts();
                        }
					};
                }

                if (flagUndoCurrent)
                {
                    currentBuilder.World.BeginUndoBatch(button.LabelText);
                    var spawnOrDestroy = currentBuilder.Current.CreateSpawnUndoPoint();
                    currentBuilder.World.EndUndoBatch();
                    spawnOrDestroy.Target.Changed += (changeable) =>
                    {
						var syncRef = changeable as ISyncRef;
                        if (syncRef.Target != null)
                        {
							//currentBuilder.ForceNext = ((Slot)syncRef.Target).GetComponent<RectTransform>();
							currentField.SetValue(currentBuilder, ((Slot)syncRef.Target) ?? ((Component)syncRef.Target).Slot);
                            UpdateTexts();
                        }
                    };
                }

                lastRoot = currentBuilder.Root;
                lastCurrent = currentBuilder.Current;

                UpdateTexts();
			}
		}
	}
}