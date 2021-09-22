using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Confuser.Core;
using Confuser.Core.Services;
using Confuser.Renamer.Analyzers;
using dnlib.DotNet;

namespace Confuser.Renamer {
	public interface INameService {
		VTableStorage GetVTables();

		void Analyze(IDnlibDef def);

		bool CanRename(object obj);
		void SetCanRename(object obj, bool val);

		void SetParam(IDnlibDef def, string name, string value);
		string GetParam(IDnlibDef def, string name);

		RenameMode GetRenameMode(object obj);
		void SetRenameMode(object obj, RenameMode val);
		void ReduceRenameMode(object obj, RenameMode val);

		string ObfuscateName(string name, RenameMode mode);
		string RandomName();
		string RandomName(RenameMode mode);

		void RegisterRenamer(IRenamer renamer);
		T FindRenamer<T>();
		void AddReference<T>(T obj, INameReference<T> reference);

		void SetOriginalName(object obj, string name);
		void SetOriginalNamespace(object obj, string ns);

		void MarkHelper(IDnlibDef def, IMarkerService marker, ConfuserComponent parentComp);
	}

	internal class NameService : INameService {
		static readonly object CanRenameKey = new object();
		static readonly object RenameModeKey = new object();
		static readonly object ReferencesKey = new object();
		static readonly object OriginalNameKey = new object();
		static readonly object OriginalNamespaceKey = new object();

		readonly ConfuserContext context;
		readonly byte[] nameSeed;
		readonly RandomGenerator random;
		readonly VTableStorage storage;
		AnalyzePhase analyze;

		readonly HashSet<string> identifiers = new HashSet<string>();
		readonly byte[] nameId = new byte[8];
		readonly Dictionary<string, string> nameMap1 = new Dictionary<string, string>();
		readonly Dictionary<string, string> nameMap2 = new Dictionary<string, string>();
		internal ReversibleRenamer reversibleRenamer;

		public NameService(ConfuserContext context) {
			this.context = context;
			storage = new VTableStorage(context.Logger);
			random = context.Registry.GetService<IRandomService>().GetRandomGenerator(NameProtection._FullId);
			nameSeed = random.NextBytes(20);

			Renamers = new List<IRenamer> {
				new InterReferenceAnalyzer(),
				new VTableAnalyzer(),
				new TypeBlobAnalyzer(),
				new ResourceAnalyzer(),
				new LdtokenEnumAnalyzer()
			};
		}

		public IList<IRenamer> Renamers { get; private set; }

		public VTableStorage GetVTables() {
			return storage;
		}

		public bool CanRename(object obj) {
			if (obj is IDnlibDef) {
				if (analyze == null)
					analyze = context.Pipeline.FindPhase<AnalyzePhase>();

				var prot = (NameProtection)analyze.Parent;
				ProtectionSettings parameters = ProtectionParameters.GetParameters(context, (IDnlibDef)obj);
				if (parameters == null || !parameters.ContainsKey(prot))
					return false;
				return context.Annotations.Get(obj, CanRenameKey, true);
			}
			return false;
		}

		public void SetCanRename(object obj, bool val) {
			context.Annotations.Set(obj, CanRenameKey, val);
		}

		public void SetParam(IDnlibDef def, string name, string value) {
			var param = ProtectionParameters.GetParameters(context, def);
			if (param == null)
				ProtectionParameters.SetParameters(context, def, param = new ProtectionSettings());
			Dictionary<string, string> nameParam;
			if (!param.TryGetValue(analyze.Parent, out nameParam))
				param[analyze.Parent] = nameParam = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			nameParam[name] = value;
		}

		public string GetParam(IDnlibDef def, string name) {
			var param = ProtectionParameters.GetParameters(context, def);
			if (param == null)
				return null;
			Dictionary<string, string> nameParam;
			if (!param.TryGetValue(analyze.Parent, out nameParam))
				return null;
			return nameParam.GetValueOrDefault(name);
		}

		public RenameMode GetRenameMode(object obj) {
			return context.Annotations.Get(obj, RenameModeKey, RenameMode.Unicode);
		}

		public void SetRenameMode(object obj, RenameMode val) {
			context.Annotations.Set(obj, RenameModeKey, val);
		}

		public void ReduceRenameMode(object obj, RenameMode val) {
			RenameMode original = GetRenameMode(obj);
			if (original < val)
				context.Annotations.Set(obj, RenameModeKey, val);
		}

		public void AddReference<T>(T obj, INameReference<T> reference) {
			context.Annotations.GetOrCreate(obj, ReferencesKey, key => new List<INameReference>()).Add(reference);
		}

		public void Analyze(IDnlibDef def) {
			if (analyze == null)
				analyze = context.Pipeline.FindPhase<AnalyzePhase>();

			SetOriginalName(def, def.Name);
			if (def is TypeDef) {
				GetVTables().GetVTable((TypeDef)def);
				SetOriginalNamespace(def, ((TypeDef)def).Namespace);
			}
			analyze.Analyze(this, context, ProtectionParameters.Empty, def, true);
		}

		public void SetNameId(uint id) {
			for (int i = nameId.Length - 1; i >= 0; i--) {
				nameId[i] = (byte)(id & 0xff);
				id >>= 8;
			}
		}

		void IncrementNameId() {
			for (int i = nameId.Length - 1; i >= 0; i--) {
				nameId[i]++;
				if (nameId[i] != 0)
					break;
			}
		}
        public static System.Random rnd = new System.Random();
        public static string RandomOrNo()
        {
            string[] charsc = { "CausalityTraceLevel", "BitConverter", "UnhandledExceptionEventHandler", "PinnedBufferMemoryStream", "RichTextBoxScrollBars", "RichTextBoxSelectionAttribute", "RichTextBoxSelectionTypes", "RichTextBoxStreamType", "RichTextBoxWordPunctuations", "RightToLeft", "RTLAwareMessageBox", "SafeNativeMethods", "SaveFileDialog", "Screen", "ScreenOrientation", "ScrollableControl", "ScrollBar", "ScrollBarRenderer", "ScrollBars", "ScrollButton", "ScrollEventArgs", "ScrollEventHandler", "ScrollEventType", "ScrollOrientation", "ScrollProperties", "SearchDirectionHint", "SearchForVirtualItemEventArgs", "SearchForVirtualItemEventHandler", "SecurityIDType", "SelectedGridItemChangedEventArgs", "SelectedGridItemChangedEventHandler", "SelectionMode", "SelectionRange", "SelectionRangeConverter", "SendKeys", "Shortcut", "SizeGripStyle", "SortOrder", "SpecialFolderEnumConverter", "SplitContainer", "Splitter", "SplitterCancelEventArgs", "SplitterCancelEventHandler", "SplitterEventArgs", "SplitterEventHandler", "SplitterPanel", "StatusBar", "StatusBarDrawItemEventArgs", "StatusBarDrawItemEventHandler", "StatusBarPanel", "StatusBarPanelAutoSize", "StatusBarPanelBorderStyle", "StatusBarPanelClickEventArgs", "StatusBarPanelClickEventHandler", "StatusBarPanelStyle", "StatusStrip", "StringSorter", "StringSource", "StructFormat", "SystemInformation", "SystemParameter", "TabAlignment", "TabAppearance", "TabControl", "TabControlAction", "TabControlCancelEventArgs", "TabControlCancelEventHandler", "TabControlEventArgs", "TabControlEventHandler", "TabDrawMode", "TableLayoutPanel", "TableLayoutControlCollection", "TableLayoutPanelCellBorderStyle", "TableLayoutPanelCellPosition", "TableLayoutPanelCellPositionTypeConverter", "TableLayoutPanelGrowStyle", "TableLayoutSettings", "SizeType", "ColumnStyle", "RowStyle", "TableLayoutStyle", "TableLayoutStyleCollection", "TableLayoutCellPaintEventArgs", "TableLayoutCellPaintEventHandler", "TableLayoutColumnStyleCollection", "TableLayoutRowStyleCollection", "TabPage", "TabRenderer", "TabSizeMode", "TextBox", "TextBoxAutoCompleteSourceConverter", "TextBoxBase", "TextBoxRenderer", "TextDataFormat", "TextImageRelation", "ThreadExceptionDialog", "TickStyle", "ToolBar", "ToolBarAppearance", "ToolBarButton", "ToolBarButtonClickEventArgs", "ToolBarButtonClickEventHandler", "ToolBarButtonStyle", "ToolBarTextAlign", "ToolStrip", "CachedItemHdcInfo", "MouseHoverTimer", "ToolStripSplitStackDragDropHandler", "ToolStripArrowRenderEventArgs", "ToolStripArrowRenderEventHandler", "ToolStripButton", "ToolStripComboBox", "ToolStripControlHost", "ToolStripDropDown", "ToolStripDropDownCloseReason", "ToolStripDropDownClosedEventArgs", "ToolStripDropDownClosedEventHandler", "ToolStripDropDownClosingEventArgs", "ToolStripDropDownClosingEventHandler", "ToolStripDropDownDirection", "ToolStripDropDownButton", "ToolStripDropDownItem", "ToolStripDropDownItemAccessibleObject", "ToolStripDropDownMenu", "ToolStripDropTargetManager", "ToolStripHighContrastRenderer", "ToolStripGrip", "ToolStripGripDisplayStyle", "ToolStripGripRenderEventArgs", "ToolStripGripRenderEventHandler", "ToolStripGripStyle", "ToolStripItem", "ToolStripItemImageIndexer", "ToolStripItemInternalLayout", "ToolStripItemAlignment", "ToolStripItemClickedEventArgs", "ToolStripItemClickedEventHandler", "ToolStripItemCollection", "ToolStripItemDisplayStyle", "ToolStripItemEventArgs", "ToolStripItemEventHandler", "ToolStripItemEventType", "ToolStripItemImageRenderEventArgs", "ToolStripItemImageRenderEventHandler", "ToolStripItemImageScaling", "ToolStripItemOverflow", "ToolStripItemPlacement", "ToolStripItemRenderEventArgs", "ToolStripItemRenderEventHandler", "ToolStripItemStates", "ToolStripItemTextRenderEventArgs", "ToolStripItemTextRenderEventHandler", "ToolStripLabel", "ToolStripLayoutStyle", "ToolStripManager", "ToolStripCustomIComparer", "MergeHistory", "MergeHistoryItem", "ToolStripManagerRenderMode", "ToolStripMenuItem", "MenuTimer", "ToolStripMenuItemInternalLayout", "ToolStripOverflow", "ToolStripOverflowButton", "ToolStripContainer", "ToolStripContentPanel", "ToolStripPanel", "ToolStripPanelCell", "ToolStripPanelRenderEventArgs", "ToolStripPanelRenderEventHandler", "ToolStripContentPanelRenderEventArgs", "ToolStripContentPanelRenderEventHandler", "ToolStripPanelRow", "ToolStripPointType", "ToolStripProfessionalRenderer", "ToolStripProfessionalLowResolutionRenderer", "ToolStripProgressBar", "ToolStripRenderer", "ToolStripRendererSwitcher", "ToolStripRenderEventArgs", "ToolStripRenderEventHandler", "ToolStripRenderMode", "ToolStripScrollButton", "ToolStripSeparator", "ToolStripSeparatorRenderEventArgs", "ToolStripSeparatorRenderEventHandler", "ToolStripSettings", "ToolStripSettingsManager", "ToolStripSplitButton", "ToolStripSplitStackLayout", "ToolStripStatusLabel", "ToolStripStatusLabelBorderSides", "ToolStripSystemRenderer", "ToolStripTextBox", "ToolStripTextDirection", "ToolStripLocationCancelEventArgs", "ToolStripLocationCancelEventHandler", "ToolTip", "ToolTipIcon", "TrackBar", "TrackBarRenderer", "TreeNode", "TreeNodeMouseClickEventArgs", "TreeNodeMouseClickEventHandler", "TreeNodeCollection", "TreeNodeConverter", "TreeNodeMouseHoverEventArgs", "TreeNodeMouseHoverEventHandler", "TreeNodeStates", "TreeView", "TreeViewAction", "TreeViewCancelEventArgs", "TreeViewCancelEventHandler", "TreeViewDrawMode", "TreeViewEventArgs", "TreeViewEventHandler", "TreeViewHitTestInfo", "TreeViewHitTestLocations", "TreeViewImageIndexConverter", "TreeViewImageKeyConverter", "Triangle", "TriangleDirection", "TypeValidationEventArgs", "TypeValidationEventHandler", "UICues", "UICuesEventArgs", "UICuesEventHandler", "UpDownBase", "UpDownEventArgs", "UpDownEventHandler", "UserControl", "ValidationConstraints", "View", "VScrollBar", "VScrollProperties", "WebBrowser", "WebBrowserEncryptionLevel", "WebBrowserReadyState", "WebBrowserRefreshOption", "WebBrowserBase", "WebBrowserContainer", "WebBrowserDocumentCompletedEventHandler", "WebBrowserDocumentCompletedEventArgs", "WebBrowserHelper", "WebBrowserNavigatedEventHandler", "WebBrowserNavigatedEventArgs", "WebBrowserNavigatingEventHandler", "WebBrowserNavigatingEventArgs", "WebBrowserProgressChangedEventHandler", "WebBrowserProgressChangedEventArgs", "WebBrowserSiteBase", "WebBrowserUriTypeConverter", "WinCategoryAttribute", "WindowsFormsSection", "WindowsFormsSynchronizationContext", "IntSecurity", "WindowsFormsUtils", "IComponentEditorPageSite", "LayoutSettings", "PageSetupDialog", "PrintControllerWithStatusDialog", "PrintDialog", "PrintPreviewControl", "PrintPreviewDialog", "TextFormatFlags", "TextRenderer", "WindowsGraphicsWrapper", "SRDescriptionAttribute", "SRCategoryAttribute", "SR", "VisualStyleElement", "VisualStyleInformation", "VisualStyleRenderer", "VisualStyleState", "ComboBoxState", "CheckBoxState", "GroupBoxState", "HeaderItemState", "PushButtonState", "RadioButtonState", "ScrollBarArrowButtonState", "ScrollBarState", "ScrollBarSizeBoxState", "TabItemState", "TextBoxState", "ToolBarState", "TrackBarThumbState", "BackgroundType", "BorderType", "ImageOrientation", "SizingType", "FillType", "HorizontalAlign", "ContentAlignment", "VerticalAlignment", "OffsetType", "IconEffect", "TextShadowType", "GlyphType", "ImageSelectType", "TrueSizeScalingType", "GlyphFontSizingType", "ColorProperty", "EnumProperty", "FilenameProperty", "FontProperty", "IntegerProperty", "PointProperty", "MarginProperty", "StringProperty", "BooleanProperty", "Edges", "EdgeStyle", "EdgeEffects", "TextMetrics", "TextMetricsPitchAndFamilyValues", "TextMetricsCharacterSet", "HitTestOptions", "HitTestCode", "ThemeSizeType", "VisualStyleDocProperty", "VisualStyleSystemProperty", "ArrayElementGridEntry", "CategoryGridEntry", "DocComment", "DropDownButton", "DropDownButtonAdapter", "GridEntry", "AttributeTypeSorter", "GridEntryRecreateChildrenEventHandler", "GridEntryRecreateChildrenEventArgs", "GridEntryCollection", "GridErrorDlg", "GridToolTip", "HotCommands", "ImmutablePropertyDescriptorGridEntry", "IRootGridEntry", "MergePropertyDescriptor", "MultiPropertyDescriptorGridEntry", "MultiSelectRootGridEntry", "PropertiesTab", "PropertyDescriptorGridEntry", "PropertyGridCommands", "PropertyGridView", "SingleSelectRootGridEntry", "ComponentEditorForm", "ComponentEditorPage", "EventsTab", "IUIService", "IWindowsFormsEditorService", "PropertyTab", "ToolStripItemDesignerAvailability", "ToolStripItemDesignerAvailabilityAttribute", "WindowsFormsComponentEditor", "BaseCAMarshaler", "Com2AboutBoxPropertyDescriptor", "Com2ColorConverter", "Com2ComponentEditor", "Com2DataTypeToManagedDataTypeConverter", "Com2Enum", "Com2EnumConverter", "Com2ExtendedBrowsingHandler", "Com2ExtendedTypeConverter", "Com2FontConverter", "Com2ICategorizePropertiesHandler", "Com2IDispatchConverter", "Com2IManagedPerPropertyBrowsingHandler", "Com2IPerPropertyBrowsingHandler", "Com2IProvidePropertyBuilderHandler", "Com2IVsPerPropertyBrowsingHandler", "Com2PictureConverter", "Com2Properties", "Com2PropertyBuilderUITypeEditor", "Com2PropertyDescriptor", "GetAttributesEvent", "Com2EventHandler", "GetAttributesEventHandler", "GetNameItemEvent", "GetNameItemEventHandler", "DynamicMetaObjectProviderDebugView", "ExpressionTreeCallRewriter", "ICSharpInvokeOrInvokeMemberBinder", "ResetBindException", "RuntimeBinder", "RuntimeBinderController", "RuntimeBinderException", "RuntimeBinderInternalCompilerException", "SpecialNames", "SymbolTable", "RuntimeBinderExtensions", "NameManager", "Name", "NameTable", "OperatorKind", "PredefinedName", "PredefinedType", "TokenFacts", "TokenKind", "OutputContext", "UNSAFESTATES", "CheckedContext", "BindingFlag", "ExpressionBinder", "BinOpKind", "BinOpMask", "CandidateFunctionMember", "ConstValKind", "CONSTVAL", "ConstValFactory", "ConvKind", "CONVERTTYPE", "BetterType", "ListExtensions", "CConversions", "Operators", "UdConvInfo", "ArgInfos", "BodyType", "ConstCastResult", "AggCastResult", "UnaryOperatorSignatureFindResult", "UnaOpKind", "UnaOpMask", "OpSigFlags", "LiftFlags", "CheckLvalueKind", "BinOpFuncKind", "UnaOpFuncKind", "ExpressionKind", "ExpressionKindExtensions", "EXPRExtensions", "ExprFactory", "EXPRFLAG", "FileRecord", "FUNDTYPE", "GlobalSymbolContext", "InputFile", "LangCompiler", "MemLookFlags", "MemberLookup", "CMemberLookupResults", "mdToken", "CorAttributeTargets", "MethodKindEnum", "MethodTypeInferrer", "NameGenerator", "CNullable", "NullableCallLiftKind", "CONSTRESKIND", "LambdaParams", "TypeOrSimpleNameResolution", "InitializerKind", "ConstantStringConcatenation", "ForeachKind", "PREDEFATTR", "PREDEFMETH", "PREDEFPROP", "MethodRequiredEnum", "MethodCallingConventionEnum", "MethodSignatureEnum", "PredefinedMethodInfo", "PredefinedPropertyInfo", "PredefinedMembers", "ACCESSERROR", "CSemanticChecker", "SubstTypeFlags", "SubstContext", "CheckConstraintsFlags", "TypeBind", "UtilityTypeExtensions", "SymWithType", "MethPropWithType", "MethWithType", "PropWithType", "EventWithType", "FieldWithType", "MethPropWithInst", "MethWithInst", "AggregateDeclaration", "Declaration", "GlobalAttributeDeclaration", "ITypeOrNamespace", "AggregateSymbol", "AssemblyQualifiedNamespaceSymbol", "EventSymbol", "FieldSymbol", "IndexerSymbol", "LabelSymbol", "LocalVariableSymbol", "MethodOrPropertySymbol", "MethodSymbol", "InterfaceImplementationMethodSymbol", "IteratorFinallyMethodSymbol", "MiscSymFactory", "NamespaceOrAggregateSymbol", "NamespaceSymbol", "ParentSymbol", "PropertySymbol", "Scope", "KAID", "ACCESS", "AggKindEnum", "ARRAYMETHOD", "SpecCons", "Symbol", "SymbolExtensions", "SymFactory", "SymFactoryBase", "SYMKIND", "SynthAggKind", "SymbolLoader", "AidContainer", "BSYMMGR", "symbmask_t", "SYMTBL", "TransparentIdentifierMemberSymbol", "TypeParameterSymbol", "UnresolvedAggregateSymbol", "VariableSymbol", "EXPRARRAYINDEX", "EXPRARRINIT", "EXPRARRAYLENGTH", "EXPRASSIGNMENT", "EXPRBINOP", "EXPRBLOCK", "EXPRBOUNDLAMBDA", "EXPRCALL", "EXPRCAST", "EXPRCLASS", "EXPRMULTIGET", "EXPRMULTI", "EXPRCONCAT", "EXPRQUESTIONMARK", "EXPRCONSTANT", "EXPREVENT", "EXPR", "ExpressionIterator", "EXPRFIELD", "EXPRFIELDINFO", "EXPRHOISTEDLOCALEXPR", "EXPRLIST", "EXPRLOCAL", "EXPRMEMGRP", "EXPRMETHODINFO", "EXPRFUNCPTR", "EXPRNamedArgumentSpecification", "EXPRPROP", "EXPRPropertyInfo", "EXPRRETURN", "EXPRSTMT", "EXPRWRAP", "EXPRTHISPOINTER", "EXPRTYPEARGUMENTS", "EXPRTYPEOF", "EXPRTYPEORNAMESPACE", "EXPRUNARYOP", "EXPRUNBOUNDLAMBDA", "EXPRUSERDEFINEDCONVERSION", "EXPRUSERLOGOP", "EXPRZEROINIT", "ExpressionTreeRewriter", "ExprVisitorBase", "AggregateType", "ArgumentListType", "ArrayType", "BoundLambdaType", "ErrorType", "MethodGroupType", "NullableType", "NullType", "OpenTypePlaceholderType", "ParameterModifierType", "PointerType", "PredefinedTypes", "PredefinedTypeFacts", "CType", "TypeArray", "TypeFactory", "TypeManager", "TypeParameterType", "KeyPair`2", "TypeTable", "VoidType", "CError", "CParameterizedError", "CErrorFactory", "ErrorFacts", "ErrArgKind", "ErrArgFlags", "SymWithTypeMemo", "MethPropWithInstMemo", "ErrArg", "ErrArgRef", "ErrArgRefOnly", "ErrArgNoRef", "ErrArgIds", "ErrArgSymKind", "ErrorHandling", "IErrorSink", "MessageID", "UserStringBuilder", "CController", "<Cons>d__10`1", "<Cons>d__11`1", "DynamicProperty", "DynamicDebugViewEmptyException", "<>c__DisplayClass20_0", "ExpressionEXPR", "ArgumentObject", "NameHashKey", "<>c__DisplayClass18_0", "<>c__DisplayClass18_1", "<>c__DisplayClass43_0", "<>c__DisplayClass45_0", "KnownName", "BinOpArgInfo", "BinOpSig", "BinOpFullSig", "ConversionFunc", "ExplicitConversion", "PfnBindBinOp", "PfnBindUnaOp", "GroupToArgsBinder", "GroupToArgsBinderResult", "ImplicitConversion", "UnaOpSig", "UnaOpFullSig", "OPINFO", "<ToEnumerable>d__1", "CMethodIterator", "NewInferenceResult", "Dependency", "<InterfaceAndBases>d__0", "<AllConstraintInterfaces>d__1", "<TypeAndBaseClasses>d__2", "<TypeAndBaseClassInterfaces>d__3", "<AllPossibleInterfaces>d__4", "<Children>d__0", "Kind", "TypeArrayKey", "Key", "PredefinedTypeInfo", "StdTypeVarColl", "<>c__DisplayClass71_0", "__StaticArrayInitTypeSize=104", "__StaticArrayInitTypeSize=169", "SNINativeMethodWrapper", "QTypes", "ProviderEnum", "IOType", "ConsumerNumber", "SqlAsyncCallbackDelegate", "ConsumerInfo", "SNI_Error", "Win32NativeMethods", "NativeOledbWrapper", "AdalException", "ADALNativeWrapper", "Sni_Consumer_Info", "SNI_ConnWrapper", "SNI_Packet_IOType", "ConsumerNum", "$ArrayType$$$BY08$$CBG", "_GUID", "SNI_CLIENT_CONSUMER_INFO", "IUnknown", "__s_GUID", "IChapteredRowset", "_FILETIME", "ProviderNum", "ITransactionLocal", "SNI_ERROR", "$ArrayType$$$BY08G", "BOID", "ModuleLoadException", "ModuleLoadExceptionHandlerException", "ModuleUninitializer", "LanguageSupport", "gcroot<System::String ^>", "$ArrayType$$$BY00Q6MPBXXZ", "Progress", "$ArrayType$$$BY0A@P6AXXZ", "$ArrayType$$$BY0A@P6AHXZ", "__enative_startup_state", "TriBool", "ICLRRuntimeHost", "ThisModule", "_EXCEPTION_POINTERS", "Bid", "SqlDependencyProcessDispatcher", "BidIdentityAttribute", "BidMetaTextAttribute", "BidMethodAttribute", "BidArgumentTypeAttribute", "ExtendedClrTypeCode", "ITypedGetters", "ITypedGettersV3", "ITypedSetters", "ITypedSettersV3", "MetaDataUtilsSmi", "SmiConnection", "SmiContext", "SmiContextFactory", "SmiEventSink", "SmiEventSink_Default", "SmiEventSink_DeferedProcessing", "SmiEventStream", "SmiExecuteType", "SmiGettersStream", "SmiLink", "SmiMetaData", "SmiExtendedMetaData", "SmiParameterMetaData", "SmiStorageMetaData", "SmiQueryMetaData", "SmiRecordBuffer", "SmiRequestExecutor", "SmiSettersStream", "SmiStream", "SmiXetterAccessMap", "SmiXetterTypeCode", "SqlContext", "SqlDataRecord", "SqlPipe", "SqlTriggerContext", "ValueUtilsSmi", "SqlClientWrapperSmiStream", "SqlClientWrapperSmiStreamChars", "IBinarySerialize", "InvalidUdtException", "SqlFacetAttribute", "DataAccessKind", "SystemDataAccessKind", "SqlFunctionAttribute", "SqlMetaData", "SqlMethodAttribute", "FieldInfoEx", "BinaryOrderedUdtNormalizer", "Normalizer", "BooleanNormalizer", "SByteNormalizer", "ByteNormalizer", "ShortNormalizer", "UShortNormalizer", "IntNormalizer", "UIntNormalizer", "LongNormalizer", "ULongNormalizer", "FloatNormalizer", "DoubleNormalizer", "SqlProcedureAttribute", "SerializationHelperSql9", "Serializer", "NormalizedSerializer", "BinarySerializeSerializer", "DummyStream", "SqlTriggerAttribute", "SqlUserDefinedAggregateAttribute", "SqlUserDefinedTypeAttribute", "TriggerAction", "MemoryRecordBuffer", "SmiPropertySelector", "SmiMetaDataPropertyCollection", "SmiMetaDataProperty", "SmiUniqueKeyProperty", "SmiOrderProperty", "SmiDefaultFieldsProperty", "SmiTypedGetterSetter", "SqlRecordBuffer", "BaseTreeIterator", "DataDocumentXPathNavigator", "DataPointer", "DataSetMapper", "IXmlDataVirtualNode", "BaseRegionIterator", "RegionIterator", "TreeIterator", "ElementState", "XmlBoundElement", "XmlDataDocument", "XmlDataImplementation", "XPathNodePointer", "AcceptRejectRule", "InternalDataCollectionBase", "TypedDataSetGenerator", "StrongTypingException", "TypedDataSetGeneratorException", "ColumnTypeConverter", "CommandBehavior", "CommandType", "KeyRestrictionBehavior", "ConflictOption", "ConnectionState", "Constraint", "ConstraintCollection", "ConstraintConverter", "ConstraintEnumerator", "ForeignKeyConstraintEnumerator", "ChildForeignKeyConstraintEnumerator", "ParentForeignKeyConstraintEnumerator", "DataColumn", "AutoIncrementValue", "AutoIncrementInt64", "AutoIncrementBigInteger", "DataColumnChangeEventArgs", "DataColumnChangeEventHandler", "DataColumnCollection", "DataColumnPropertyDescriptor", "DataError", "DataException", "ConstraintException", "DeletedRowInaccessibleException", "DuplicateNameException", "InRowChangingEventException", "InvalidConstraintException", "MissingPrimaryKeyException", "NoNullAllowedException", "ReadOnlyException", "RowNotInTableException", "VersionNotFoundException", "ExceptionBuilder", "DataKey", "DataRelation", "DataRelationCollection", "DataRelationPropertyDescriptor", "DataRow", "DataRowBuilder", "DataRowAction", "DataRowChangeEventArgs", "DataRowChangeEventHandler", "DataRowCollection", "DataRowCreatedEventHandler", "DataSetClearEventhandler", "DataRowState", "DataRowVersion", "DataRowView", "SerializationFormat", "DataSet", "DataSetSchemaImporterExtension", "DataSetDateTime", "DataSysDescriptionAttribute", "DataTable", "DataTableClearEventArgs", "DataTableClearEventHandler", "DataTableCollection", "DataTableNewRowEventArgs", "DataTableNewRowEventHandler", "DataTablePropertyDescriptor", "DataTableReader", "DataTableReaderListener", "DataTableTypeConverter", "DataView", "DataViewListener", "DataViewManager", "DataViewManagerListItemTypeDescriptor", "DataViewRowState", "DataViewSetting", "DataViewSettingCollection", "DBConcurrencyException", "DbType", "DefaultValueTypeConverter", "FillErrorEventArgs", "FillErrorEventHandler", "AggregateNode", "BinaryNode", "LikeNode", "ConstNode", "DataExpression", "ExpressionNode", "ExpressionParser", "Tokens", "OperatorInfo", "InvalidExpressionException", "EvaluateException", "SyntaxErrorException", "ExprException", "FunctionNode", "FunctionId", "Function", "IFilter", "LookupNode", "NameNode", "UnaryNode", "ZeroOpNode", "ForeignKeyConstraint", "IColumnMapping", "IColumnMappingCollection", "IDataAdapter", "IDataParameter", "IDataParameterCollection", "IDataReader", "IDataRecord", "IDbCommand", "IDbConnection", "IDbDataAdapter", "IDbDataParameter", "IDbTransaction", "IsolationLevel", "ITableMapping", "ITableMappingCollection", "LoadOption", "MappingType", "MergeFailedEventArgs", "MergeFailedEventHandler", "Merger", "MissingMappingAction", "MissingSchemaAction", "OperationAbortedException", "ParameterDirection", "PrimaryKeyTypeConverter", "PropertyCollection", "RBTreeError", "TreeAccessMethod", "RBTree`1", "RecordManager", "StatementCompletedEventArgs", "StatementCompletedEventHandler", "RelatedView", "RelationshipConverter", "Rule", "SchemaSerializationMode", "SchemaType", "IndexField", "Index", "Listeners`1", "SimpleType", "LocalDBAPI", "LocalDBInstanceElement", "LocalDBInstancesCollection", "LocalDBConfigurationSection", "SqlDbType", "StateChangeEventArgs", "StateChangeEventHandler", "StatementType", "UniqueConstraint", "UpdateRowSource", "UpdateStatus", "XDRSchema", "XmlDataLoader", "XMLDiffLoader", "XmlReadMode", "SchemaFormat", "XmlTreeGen", "NewDiffgramGen", "XmlDataTreeWriter", "DataTextWriter", "DataTextReader", "XMLSchema", "ConstraintTable", "XSDSchema", "XmlIgnoreNamespaceReader", "XmlToDatasetMap", "XmlWriteMode", "SqlEventSource", "SqlDataSourceEnumerator", "SqlGenericUtil", "SqlNotificationRequest", "INullable", "SqlBinary", "SqlBoolean", "SqlByte", "SqlBytesCharsState", "SqlBytes", "StreamOnSqlBytes", "SqlChars", "StreamOnSqlChars", "SqlStreamChars", "SqlDateTime", "SqlDecimal", "SqlDouble", "SqlFileStream", "UnicodeString", "SecurityQualityOfService", "FileFullEaInformation", "SqlGuid", "SqlInt16", "SqlInt32", "SqlInt64", "SqlMoney", "SQLResource", "SqlSingle", "SqlCompareOptions", "SqlString", "SqlTypesSchemaImporterExtensionHelper", "TypeCharSchemaImporterExtension", "TypeNCharSchemaImporterExtension", "TypeVarCharSchemaImporterExtension", "TypeNVarCharSchemaImporterExtension", "TypeTextSchemaImporterExtension", "TypeNTextSchemaImporterExtension", "TypeVarBinarySchemaImporterExtension", "TypeBinarySchemaImporterExtension", "TypeVarImageSchemaImporterExtension", "TypeDecimalSchemaImporterExtension", "TypeNumericSchemaImporterExtension", "TypeBigIntSchemaImporterExtension", "TypeIntSchemaImporterExtension", "TypeSmallIntSchemaImporterExtension", "TypeTinyIntSchemaImporterExtension", "TypeBitSchemaImporterExtension", "TypeFloatSchemaImporterExtension", "TypeRealSchemaImporterExtension", "TypeDateTimeSchemaImporterExtension", "TypeSmallDateTimeSchemaImporterExtension", "TypeMoneySchemaImporterExtension", "TypeSmallMoneySchemaImporterExtension", "TypeUniqueIdentifierSchemaImporterExtension", "EComparison", "StorageState", "SqlTypeException", "SqlNullValueException", "SqlTruncateException", "SqlNotFilledException", "SqlAlreadyFilledException", "SQLDebug", "SqlXml", "SqlXmlStreamWrapper", "SqlClientEncryptionAlgorithmFactoryList", "SqlSymmetricKeyCache", "SqlColumnEncryptionKeyStoreProvider", "SqlColumnEncryptionCertificateStoreProvider", "SqlColumnEncryptionCngProvider", "SqlColumnEncryptionCspProvider", "SqlAeadAes256CbcHmac256Algorithm", "SqlAeadAes256CbcHmac256Factory", "SqlAeadAes256CbcHmac256EncryptionKey", "SqlAes256CbcAlgorithm", "SqlAes256CbcFactory", "SqlClientEncryptionAlgorithm", "SqlClientEncryptionAlgorithmFactory", "SqlClientEncryptionType", "SqlClientSymmetricKey", "SqlSecurityUtility", "SqlQueryMetadataCache", "ApplicationIntent", "SqlCredential", "SqlConnectionPoolKey", "AssemblyCache", "OnChangeEventHandler", "SqlRowsCopiedEventArgs", "SqlRowsCopiedEventHandler", "SqlBuffer", "_ColumnMapping", "Row", "BulkCopySimpleResultSet", "SqlBulkCopy", "SqlBulkCopyColumnMapping", "SqlBulkCopyColumnMappingCollection", "SqlBulkCopyOptions", "SqlCachedBuffer", "SqlClientFactory", "SqlClientMetaDataCollectionNames", "SqlClientPermission", "SqlClientPermissionAttribute", "SqlCommand", "SqlCommandBuilder", "SqlCommandSet", "SqlConnection", "SQLDebugging", "ISQLDebug", "SqlDebugContext", "MEMMAP", "SqlConnectionFactory", "SqlPerformanceCounters", "SqlConnectionPoolGroupProviderInfo", "SqlConnectionPoolProviderInfo", "SqlConnectionString", "SqlConnectionStringBuilder", "SqlConnectionTimeoutErrorPhase", "SqlConnectionInternalSourceType", "SqlConnectionTimeoutPhaseDuration", "SqlConnectionTimeoutErrorInternal", "SqlDataAdapter", "SqlDataReader", "SqlDataReaderSmi", "SqlDelegatedTransaction", "SqlDependency", "SqlDependencyPerAppDomainDispatcher", "SqlNotification", "MetaType", "TdsDateTime", "SqlError", "SqlErrorCollection", "SqlException", "SqlInfoMessageEventArgs", "SqlInfoMessageEventHandler", "SqlInternalConnection", "SqlInternalConnectionSmi", "SessionStateRecord", "SessionData", "SqlInternalConnectionTds", "ServerInfo", "TransactionState", "TransactionType", "SqlInternalTransaction", "SqlMetaDataFactory", "SqlNotificationEventArgs", "SqlNotificationInfo", "SqlNotificationSource", "SqlNotificationType", "DataFeed", "StreamDataFeed", "TextDataFeed", "XmlDataFeed", "SqlParameter", "SqlParameterCollection", "SqlReferenceCollection", "SqlRowUpdatedEventArgs", "SqlRowUpdatedEventHandler", "SqlRowUpdatingEventArgs", "SqlRowUpdatingEventHandler", "SqlSequentialStream", "SqlSequentialStreamSmi", "System.Diagnostics.DebuggableAttribute", "System.Diagnostics", "System.Net.WebClient", "System", "System.Specialized.Protection" };
            return charsc[rnd.Next(charsc.Length)];
        }
        string ObfuscateNameInternal(byte[] hash, RenameMode mode) {
			switch (mode) {
				case RenameMode.Empty:
					return "";
				case RenameMode.Unicode:
                    return RandomOrNo();
                case RenameMode.Letters:
                    return RandomOrNo();
                case RenameMode.ASCII:
                    return RandomOrNo();
                case RenameMode.Decodable:
					IncrementNameId();
                    return RandomOrNo();
                case RenameMode.Sequential:
					IncrementNameId();
                    return RandomOrNo();
                default:

					throw new NotSupportedException("Rename mode '" + mode + "' is not supported.");
			}
		}

		string ParseGenericName(string name, out int? count) {
			if (name.LastIndexOf('`') != -1) {
				int index = name.LastIndexOf('`');
				int c;
				if (int.TryParse(name.Substring(index + 1), out c)) {
					count = c;
					return name.Substring(0, index);
				}
			}
			count = null;
			return name;
		}

		string MakeGenericName(string name, int? count) {
			if (count == null)
				return name;
			else
				return string.Format("{0}`{1}", name, count.Value);
		}

		public string ObfuscateName(string name, RenameMode mode) {
			string newName = null;
			int? count;
			name = ParseGenericName(name, out count);

			if (string.IsNullOrEmpty(name))
				return string.Empty;

			if (mode == RenameMode.Empty)
				return "";
			if (mode == RenameMode.Debug)
				return "_" + name;
			if (mode == RenameMode.Reversible) {
				if (reversibleRenamer == null)
					throw new ArgumentException("Password not provided for reversible renaming.");
				newName = reversibleRenamer.Encrypt(name);
				return MakeGenericName(newName, count);
			}

			if (nameMap1.ContainsKey(name))
				return nameMap1[name];

			byte[] hash = Utils.Xor(Utils.SHA1(Encoding.UTF8.GetBytes(name)), nameSeed);
			for (int i = 0; i < 100; i++) {
				newName = ObfuscateNameInternal(hash, mode);
				if (!identifiers.Contains(MakeGenericName(newName, count)))
					break;
				hash = Utils.SHA1(hash);
			}

			if ((mode & RenameMode.Decodable) != 0) {
				nameMap2[newName] = name;
				nameMap1[name] = newName;
			}

			return MakeGenericName(newName, count);
		}

		public string RandomName() {
			return RandomName(RenameMode.Unicode);
		}

		public string RandomName(RenameMode mode) {
			return ObfuscateName(Utils.ToHexString(random.NextBytes(16)), mode);
		}

		public void SetOriginalName(object obj, string name) {
			identifiers.Add(name);
			context.Annotations.Set(obj, OriginalNameKey, name);
		}

		public void SetOriginalNamespace(object obj, string ns) {
			identifiers.Add(ns);
			context.Annotations.Set(obj, OriginalNamespaceKey, ns);
		}

		public void RegisterRenamer(IRenamer renamer) {
			Renamers.Add(renamer);
		}

		public T FindRenamer<T>() {
			return Renamers.OfType<T>().Single();
		}

		public void MarkHelper(IDnlibDef def, IMarkerService marker, ConfuserComponent parentComp) {
			if (marker.IsMarked(def))
				return;
			if (def is MethodDef) {
				var method = (MethodDef)def;
				method.Access = MethodAttributes.Assembly;
				if (!method.IsSpecialName && !method.IsRuntimeSpecialName && !method.DeclaringType.IsDelegate())
					method.Name = RandomName();
			}
			else if (def is FieldDef) {
				var field = (FieldDef)def;
				field.Access = FieldAttributes.Assembly;
				if (!field.IsSpecialName && !field.IsRuntimeSpecialName)
					field.Name = RandomName();
			}
			else if (def is TypeDef) {
				var type = (TypeDef)def;
				type.Visibility = type.DeclaringType == null ? TypeAttributes.NotPublic : TypeAttributes.NestedAssembly;
				type.Namespace = "";
				if (!type.IsSpecialName && !type.IsRuntimeSpecialName)
					type.Name = RandomName();
			}
			SetCanRename(def, false);
			Analyze(def);
			marker.Mark(def, parentComp);
		}

		#region Charsets

		static readonly char[] asciiCharset = Enumerable.Range(32, 95)
		                                                .Select(ord => (char)ord)
		                                                .Except(new[] { '.' })
		                                                .ToArray();

		static readonly char[] letterCharset = Enumerable.Range(0, 26)
		                                                 .SelectMany(ord => new[] { (char)('a' + ord), (char)('A' + ord) })
		                                                 .ToArray();

		static readonly char[] alphaNumCharset = Enumerable.Range(0, 26)
		                                                   .SelectMany(ord => new[] { (char)('a' + ord), (char)('A' + ord) })
		                                                   .Concat(Enumerable.Range(0, 10).Select(ord => (char)('0' + ord)))
		                                                   .ToArray();

		// Especially chosen, just to mess with people.
		// Inspired by: http://xkcd.com/1137/ :D
		static readonly char[] unicodeCharset = new char[] { }
			.Concat(Enumerable.Range(0x200b, 5).Select(ord => (char)ord))
			.Concat(Enumerable.Range(0x2029, 6).Select(ord => (char)ord))
			.Concat(Enumerable.Range(0x206a, 6).Select(ord => (char)ord))
			.Except(new[] { '\u2029' })
			.ToArray();

		#endregion

		public RandomGenerator GetRandom() {
			return random;
		}

		public IList<INameReference> GetReferences(object obj) {
			return context.Annotations.GetLazy(obj, ReferencesKey, key => new List<INameReference>());
		}

		public string GetOriginalName(object obj) {
			return context.Annotations.Get(obj, OriginalNameKey, "");
		}

		public string GetOriginalNamespace(object obj) {
			return context.Annotations.Get(obj, OriginalNamespaceKey, "");
		}

		public ICollection<KeyValuePair<string, string>> GetNameMap() {
			return nameMap2;
		}
	}
}