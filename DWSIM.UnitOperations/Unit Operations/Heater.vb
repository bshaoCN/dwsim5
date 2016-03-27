﻿'    Heater Calculation Routines 
'    Copyright 2008 Daniel Wagner O. de Medeiros
'
'    This file is part of DWSIM.
'
'    DWSIM is free software: you can redistribute it and/or modify
'    it under the terms of the GNU General Public License as published by
'    the Free Software Foundation, either version 3 of the License, or
'    (at your option) any later version.
'
'    DWSIM is distributed in the hope that it will be useful,
'    but WITHOUT ANY WARRANTY; without even the implied warranty of
'    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
'    GNU General Public License for more details.
'
'    You should have received a copy of the GNU General Public License
'    along with DWSIM.  If not, see <http://www.gnu.org/licenses/>.

Imports DWSIM.DrawingTools.GraphicObjects
Imports DWSIM.DWSIM.Flowsheet.FlowSheetSolver

Namespace UnitOperations

    <System.Serializable()> Public Class Heater

        Inherits SharedClasses.UnitOperations.BaseClass

        Public Enum CalculationMode
            HeatAdded = 0
            OutletTemperature = 1
            EnergyStream = 2
            OutletVaporFraction = 3
        End Enum

        Protected m_dp As Nullable(Of Double)
        Protected m_dt As Nullable(Of Double)
        Protected m_DQ As Nullable(Of Double)
        Protected m_Tout As Nullable(Of Double) = 298.15#
        Protected m_VFout As Nullable(Of Double)
        Protected m_cmode As CalculationMode = CalculationMode.HeatAdded

        Protected m_eta As Nullable(Of Double) = 100

        Protected m_FixOnHeat As Boolean = True

        Public Property FixOnHeat() As Boolean
            Get
                Return m_FixOnHeat
            End Get
            Set(ByVal value As Boolean)
                m_FixOnHeat = value
            End Set
        End Property

        Public Sub New(ByVal name As String, ByVal description As String)
            MyBase.CreateNew()
            Me.ComponentName = name
            Me.ComponentDescription = description


        End Sub

        Public Property Eficiencia() As Nullable(Of Double)
            Get
                Return m_eta
            End Get
            Set(ByVal value As Nullable(Of Double))
                m_eta = value
            End Set
        End Property

        Public Property OutletVaporFraction() As Nullable(Of Double)
            Get
                Return m_VFout
            End Get
            Set(ByVal value As Nullable(Of Double))
                m_VFout = value
            End Set
        End Property

        Public Property OutletTemperature() As Nullable(Of Double)
            Get
                Return m_Tout
            End Get
            Set(ByVal value As Nullable(Of Double))
                m_Tout = value
            End Set
        End Property

        Public Property CalcMode() As CalculationMode
            Get
                Return m_cmode
            End Get
            Set(ByVal value As CalculationMode)
                m_cmode = value
            End Set
        End Property


        Public Property DeltaP() As Nullable(Of Double)
            Get
                Return m_dp
            End Get
            Set(ByVal value As Nullable(Of Double))
                m_dp = value
            End Set
        End Property

        Public Property DeltaT() As Nullable(Of Double)
            Get
                Return m_dt
            End Get
            Set(ByVal value As Nullable(Of Double))
                m_dt = value
            End Set
        End Property

        Public Property DeltaQ() As Nullable(Of Double)
            Get
                Return m_DQ
            End Get
            Set(ByVal value As Nullable(Of Double))
                m_DQ = value
            End Set
        End Property

        Public Sub New()
            MyBase.New()
        End Sub

        Public Overrides Function Calculate(Optional ByVal args As Object = Nothing) As Integer

            Dim form As Global.DWSIM.FormFlowsheet = Me.Flowsheet
            Dim objargs As New DWSIM.Extras.StatusChangeEventArgs

            If Not Me.GraphicObject.InputConnectors(1).IsAttached Then
                'Call function to calculate flowsheet
                With objargs
                    .Calculated = False
                    .Name = Me.Name
                    .ObjectType = ObjectType.Heater
                End With

                Throw New Exception(Me.FlowSheet.GetTranslatedString("NohcorrentedeEnergyFlow2"))
            ElseIf Not Me.GraphicObject.OutputConnectors(0).IsAttached Then
                'Call function to calculate flowsheet
                With objargs
                    .Calculated = False
                    .Name = Me.Name
                    .ObjectType = ObjectType.Heater
                End With

                Throw New Exception(Me.FlowSheet.GetTranslatedString("Verifiqueasconexesdo"))
            ElseIf Not Me.GraphicObject.InputConnectors(0).IsAttached Then
                'Call function to calculate flowsheet
                With objargs
                    .Calculated = False
                    .Name = Me.Name
                    .ObjectType = ObjectType.Heater
                End With

                Throw New Exception(Me.FlowSheet.GetTranslatedString("Verifiqueasconexesdo"))
            End If

            form.Collections.FlowsheetObjectCollection(Me.GraphicObject.InputConnectors(0).AttachedConnector.AttachedFrom.Name).Validate()

            Dim Ti, Pi, Hi, Wi, ei, ein, T2, P2, H2, V2 As Double

            Me.PropertyPackage.CurrentMaterialStream = form.Collections.FlowsheetObjectCollection(Me.GraphicObject.InputConnectors(0).AttachedConnector.AttachedFrom.Name)
            Ti = form.Collections.FlowsheetObjectCollection(Me.GraphicObject.InputConnectors(0).AttachedConnector.AttachedFrom.Name).Phases(0).Properties.temperature.GetValueOrDefault
            Pi = form.Collections.FlowsheetObjectCollection(Me.GraphicObject.InputConnectors(0).AttachedConnector.AttachedFrom.Name).Phases(0).Properties.pressure.GetValueOrDefault
            Hi = form.Collections.FlowsheetObjectCollection(Me.GraphicObject.InputConnectors(0).AttachedConnector.AttachedFrom.Name).Phases(0).Properties.enthalpy.GetValueOrDefault
            Wi = form.Collections.FlowsheetObjectCollection(Me.GraphicObject.InputConnectors(0).AttachedConnector.AttachedFrom.Name).Phases(0).Properties.massflow.GetValueOrDefault
            ei = Hi * Wi
            ein = ei

            Dim es As DWSIM.SimulationObjects.Streams.EnergyStream = form.Collections.FlowsheetObjectCollection(Me.GraphicObject.InputConnectors(1).AttachedConnector.AttachedFrom.Name)

            P2 = Pi - Me.DeltaP.GetValueOrDefault

            If DebugMode Then AppendDebugLine(String.Format("Property Package: {0}", Me.PropertyPackage.ComponentName))
            If DebugMode Then AppendDebugLine(String.Format("Flash Algorithm: {0}", Me.PropertyPackage.FlashBase.GetType.Name))
            If DebugMode Then AppendDebugLine(String.Format("Input variables: T = {0} K, P = {1} Pa, H = {2} kJ/kg, W = {3} kg/s", Ti, Pi, Hi, Wi))

            If DebugMode Then AppendDebugLine("Calculation mode: " & CalcMode.ToString)

            Select Case Me.CalcMode

                Case CalculationMode.HeatAdded

                    H2 = Me.DeltaQ.GetValueOrDefault * (Me.Eficiencia.GetValueOrDefault / 100) / Wi + Hi

                    If DebugMode Then AppendDebugLine(String.Format("Doing a PH flash to calculate outlet temperature... P = {0} Pa, H = {1} kJ/[kg.K]", P2, H2))

                    Dim tmp = Me.PropertyPackage.DW_CalcEquilibrio_ISOL(PropertyPackages.FlashSpec.P, PropertyPackages.FlashSpec.H, P2, H2, 0)
                    T2 = tmp(2)
                    Me.DeltaT = T2 - Ti

                    If DebugMode Then AppendDebugLine(String.Format("Calculated outlet temperature T2 = {0} K", T2))

                    'Corrente de EnergyFlow - atualizar valor da potência (kJ/s)
                    With es
                        .EnergyFlow = Me.DeltaQ.GetValueOrDefault
                        .GraphicObject.Calculated = True
                    End With

                Case CalculationMode.OutletTemperature

                    T2 = Me.OutletTemperature.GetValueOrDefault

                    If DebugMode Then AppendDebugLine(String.Format("Doing a PT flash to calculate outlet enthalpy... P = {0} Pa, T = {1} K", P2, T2))

                    Dim tmp = Me.PropertyPackage.DW_CalcEquilibrio_ISOL(PropertyPackages.FlashSpec.T, PropertyPackages.FlashSpec.P, T2, P2, 0)
                    H2 = tmp(4)
                    CheckSpec(H2, False, "outlet enthalpy")
                    Me.DeltaT = T2 - Ti
                    Me.DeltaQ = (H2 - Hi) / (Me.Eficiencia.GetValueOrDefault / 100) * Wi

                    'Corrente de EnergyFlow - atualizar valor da potência (kJ/s)
                    With es
                        .EnergyFlow = Me.DeltaQ.GetValueOrDefault
                        .GraphicObject.Calculated = True
                    End With

                Case CalculationMode.EnergyStream

                    Me.DeltaQ = es.EnergyFlow.GetValueOrDefault
                    H2 = Me.DeltaQ.GetValueOrDefault * (Me.Eficiencia.GetValueOrDefault / 100) / Wi + Hi

                    If DebugMode Then AppendDebugLine(String.Format("Doing a PH flash to calculate outlet temperature... P = {0} Pa, H = {1} kJ/[kg.K]", P2, H2))

                    Dim tmp = Me.PropertyPackage.DW_CalcEquilibrio_ISOL(PropertyPackages.FlashSpec.P, PropertyPackages.FlashSpec.H, P2, H2, Ti)
                    T2 = tmp(2)
                    CheckSpec(T2, True, "outlet temperature")
                    Me.DeltaT = T2 - Ti

                    If DebugMode Then AppendDebugLine(String.Format("Calculated outlet temperature T2 = {0} K", T2))

                Case CalculationMode.OutletVaporFraction

                    V2 = m_VFout.GetValueOrDefault

                    If DebugMode Then AppendDebugLine(String.Format("Doing a PVF flash to calculate outlet temperature... P = {0} Pa, VF = {1}", P2, V2))

                    Dim tmp = Me.PropertyPackage.DW_CalcEquilibrio_ISOL(PropertyPackages.FlashSpec.P, PropertyPackages.FlashSpec.VAP, P2, m_VFout.GetValueOrDefault, Ti)
                    H2 = tmp(4)
                    CheckSpec(H2, False, "outlet enthalpy")
                    T2 = tmp(2)
                    CheckSpec(T2, True, "outlet temperature")
                    Me.DeltaT = T2 - Ti
                    Me.DeltaQ = (H2 - Hi) / (Me.Eficiencia.GetValueOrDefault / 100) * Wi

                    If DebugMode Then AppendDebugLine(String.Format("Calculated outlet temperature T2 = {0} K", T2))

                    'Corrente de EnergyFlow - atualizar valor da potência (kJ/s)
                    With es
                        .EnergyFlow = Me.DeltaQ.GetValueOrDefault
                        .GraphicObject.Calculated = True
                    End With

            End Select

            If Not DebugMode Then

                'Atribuir valores à corrente de matéria conectada à jusante
                Dim omstr As MaterialStream = form.Collections.FlowsheetObjectCollection(Me.GraphicObject.OutputConnectors(0).AttachedConnector.AttachedTo.Name)
                With omstr
                    .Phases(0).Properties.temperature = T2
                    .Phases(0).Properties.pressure = P2
                    .Phases(0).Properties.enthalpy = H2
                    .Phases(2).Properties.molarfraction = V2
                    Dim comp As DWSIM.Thermodynamics.BaseClasses.Compound
                    For Each comp In .Phases(0).Compounds.Values
                        comp.MoleFraction = form.Collections.FlowsheetObjectCollection(Me.GraphicObject.InputConnectors(0).AttachedConnector.AttachedFrom.Name).Phases(0).Compounds(comp.Name).MoleFraction
                        comp.MassFraction = form.Collections.FlowsheetObjectCollection(Me.GraphicObject.InputConnectors(0).AttachedConnector.AttachedFrom.Name).Phases(0).Compounds(comp.Name).MassFraction
                    Next
                    .Phases(0).Properties.massflow = form.Collections.FlowsheetObjectCollection(Me.GraphicObject.InputConnectors(0).AttachedConnector.AttachedFrom.Name).Phases(0).Properties.massflow.GetValueOrDefault
                End With

                'Call function to calculate flowsheet
                With objargs
                    .Calculated = True
                    .Name = Me.Name
                    .Tag = Me.GraphicObject.Tag
                    .ObjectType = ObjectType.Heater
                End With

                form.CalculationQueue.Enqueue(objargs)

            Else

                AppendDebugLine("Calculation finished successfully.")

            End If

        End Function

        Public Overrides Function DeCalculate() As Integer

            Dim form As Global.DWSIM.FormFlowsheet = Me.Flowsheet

            If Me.GraphicObject.OutputConnectors(0).IsAttached Then

                'Zerar valores da corrente de matéria conectada a jusante
                With form.Collections.FlowsheetObjectCollection(Me.GraphicObject.OutputConnectors(0).AttachedConnector.AttachedTo.Name)
                    .Phases(0).Properties.temperature = Nothing
                    .Phases(0).Properties.pressure = Nothing
                    .Phases(0).Properties.enthalpy = Nothing
                    .Phases(0).Properties.molarfraction = 1
                    .Phases(0).Properties.massfraction = 1
                    Dim comp As DWSIM.Thermodynamics.BaseClasses.Compound
                    Dim i As Integer = 0
                    For Each comp In .Phases(0).Compounds.Values
                        comp.MoleFraction = 0
                        comp.MassFraction = 0
                        i += 1
                    Next
                    .Phases(0).Properties.massflow = Nothing
                    .Phases(0).Properties.molarflow = Nothing
                    .GraphicObject.Calculated = False
                End With

            End If

            'Corrente de EnergyFlow - atualizar valor da potência (kJ/s)
            If Me.GraphicObject.EnergyConnector.IsAttached Then
                With form.Collections.FlowsheetObjectCollection(Me.GraphicObject.EnergyConnector.AttachedConnector.AttachedTo.Name)
                    .EnergyFlow = Nothing
                    .GraphicObject.Calculated = False
                End With
            End If

            'Call function to calculate flowsheet
            Dim objargs As New DWSIM.Extras.StatusChangeEventArgs
            With objargs
                .Calculated = False
                .Name = Me.Name
                .Tag = Me.GraphicObject.Tag
                .ObjectType = ObjectType.Heater
            End With

            form.CalculationQueue.Enqueue(objargs)

        End Function

        Public Overrides Sub PopulatePropertyGrid(ByVal pgrid As PropertyGridEx.PropertyGridEx, ByVal su As SystemsOfUnits.Units)

            Dim Conversor As New SystemsOfUnits.Converter

            With pgrid

                .PropertySort = PropertySort.Categorized
                .ShowCustomProperties = True
                .Item.Clear()

                MyBase.PopulatePropertyGrid(pgrid, su)

                Dim ent, saida, energ As String
                If Me.GraphicObject.InputConnectors(0).IsAttached = True Then
                    ent = Me.GraphicObject.InputConnectors(0).AttachedConnector.AttachedFrom.Tag
                Else
                    ent = ""
                End If
                If Me.GraphicObject.OutputConnectors(0).IsAttached = True Then
                    saida = Me.GraphicObject.OutputConnectors(0).AttachedConnector.AttachedTo.Tag
                Else
                    saida = ""
                End If

                If Me.GraphicObject.InputConnectors(1).IsAttached = True Then
                    energ = Me.GraphicObject.InputConnectors(1).AttachedConnector.AttachedFrom.Tag
                Else
                    energ = ""
                End If

                .Item.Add(Me.FlowSheet.GetTranslatedString("Correntedeentrada"), ent, False, Me.FlowSheet.GetTranslatedString("Conexes1"), "", True)
                With .Item(.Item.Count - 1)
                    .DefaultValue = Nothing
                    .CustomEditor = New DWSIM.Editors.Streams.UIInputMSSelector
                End With

                .Item.Add(Me.FlowSheet.GetTranslatedString("Correntedesada"), saida, False, Me.FlowSheet.GetTranslatedString("Conexes1"), "", True)
                With .Item(.Item.Count - 1)
                    .DefaultValue = Nothing
                    .CustomEditor = New DWSIM.Editors.Streams.UIOutputMSSelector
                End With

                .Item.Add(Me.FlowSheet.GetTranslatedString("CorrentedeEnergia"), energ, False, Me.FlowSheet.GetTranslatedString("Conexes1"), "", True)
                With .Item(.Item.Count - 1)
                    .DefaultValue = Nothing
                    .CustomEditor = New DWSIM.Editors.Streams.UIInputESSelector
                End With

                Dim valor = Format(SystemsOfUnits.Converter.ConvertFromSI(su.deltaP, Me.DeltaP.GetValueOrDefault), FlowSheet.Options.NumberFormat)
                .Item.Add(FT(Me.FlowSheet.GetTranslatedString("Quedadepresso"), su.deltaP), Double.Parse(valor), False, Me.FlowSheet.GetTranslatedString("Parmetrosdeclculo2"), Me.FlowSheet.GetTranslatedString("Quedadepressoaplicad3"), True)
                With .Item(.Item.Count - 1)
                    .CustomTypeConverter = New System.ComponentModel.StringConverter
                    .Tag2 = "PROP_HT_0"
                    .Tag = New Object() {FlowSheet.Options.NumberFormat, su.deltaP, "DP"}
                    .CustomEditor = New DWSIM.Editors.Generic.UIUnitConverter
                End With


                .Item.Add(Me.FlowSheet.GetTranslatedString("HeaterCoolerCalcMode"), Me, "CalcMode", False, Me.FlowSheet.GetTranslatedString("Parmetrosdeclculo2"), "", True)
                .Item(.Item.Count - 1).Tag2 = "CalcMode"

                Select Case Me.CalcMode

                    Case CalculationMode.EnergyStream

                    Case CalculationMode.HeatAdded

                        valor = Format(SystemsOfUnits.Converter.ConvertFromSI(su.heatflow, Me.DeltaQ.GetValueOrDefault), FlowSheet.Options.NumberFormat)
                        .Item.Add(FT(Me.FlowSheet.GetTranslatedString("CalorFornecido"), su.heatflow), Double.Parse(valor), False, Me.FlowSheet.GetTranslatedString("Parmetrosdeclculo2"), Me.FlowSheet.GetTranslatedString("Quantidadedecalorced"), True)
                        With .Item(.Item.Count - 1)
                            .CustomTypeConverter = New System.ComponentModel.StringConverter
                            .Tag2 = "PROP_HT_3"
                            .Tag = New Object() {FlowSheet.Options.NumberFormat, su.heatflow, "E"}
                            .CustomEditor = New DWSIM.Editors.Generic.UIUnitConverter
                        End With

                    Case CalculationMode.OutletTemperature

                        valor = Format(SystemsOfUnits.Converter.ConvertFromSI(su.temperature, Me.OutletTemperature.GetValueOrDefault), FlowSheet.Options.NumberFormat)
                        .Item.Add(FT(Me.FlowSheet.GetTranslatedString("HeaterCoolerOutletTemperature"), su.temperature), Double.Parse(valor), False, Me.FlowSheet.GetTranslatedString("Parmetrosdeclculo2"), "", True)
                        With .Item(.Item.Count - 1)
                            .CustomTypeConverter = New System.ComponentModel.StringConverter
                            .Tag2 = "PROP_HT_2"
                            .Tag = New Object() {FlowSheet.Options.NumberFormat, su.temperature, "T"}
                            .CustomEditor = New DWSIM.Editors.Generic.UIUnitConverter
                        End With

                    Case CalculationMode.OutletVaporFraction

                        valor = Format(Me.OutletVaporFraction.GetValueOrDefault, FlowSheet.Options.NumberFormat)
                        .Item.Add(Me.FlowSheet.GetTranslatedString("FraomolardaPhasePhaseV"), Double.Parse(valor), False, Me.FlowSheet.GetTranslatedString("Parmetrosdeclculo2"), "", True)
                        With .Item(.Item.Count - 1)
                            .Tag2 = "PROP_HT_4"
                        End With

                End Select

                .Item.Add(Me.FlowSheet.GetTranslatedString("Eficincia0100"), Me, "Eficiencia", False, Me.FlowSheet.GetTranslatedString("Parmetrosdeclculo2"), Me.FlowSheet.GetTranslatedString("Eficinciadoaquecedor"), True)
                With .Item(.Item.Count - 1)
                    .Tag2 = "PROP_HT_1"
                    .DefaultValue = Nothing
                    .DefaultType = GetType(Nullable(Of Double))
                End With

                If Me.GraphicObject.Calculated And Not Me.CalcMode = CalculationMode.HeatAdded Then
                    valor = Format(SystemsOfUnits.Converter.ConvertFromSI(su.heatflow, Me.DeltaQ.GetValueOrDefault), FlowSheet.Options.NumberFormat)
                    .Item.Add(FT(Me.FlowSheet.GetTranslatedString("CalorFornecido"), su.heatflow), valor, True, Me.FlowSheet.GetTranslatedString("Resultados3"), Me.FlowSheet.GetTranslatedString("Quantidadedecalorced"), True)
                End If

                .Item.Add(FT(Me.FlowSheet.GetTranslatedString("DeltaT2"), su.deltaT), Format(SystemsOfUnits.Converter.ConvertFromSI(su.deltaT, Me.DeltaT.GetValueOrDefault), FlowSheet.Options.NumberFormat), True, Me.FlowSheet.GetTranslatedString("Resultados3"), Me.FlowSheet.GetTranslatedString("Diferenadetemperatur"), True)
                With .Item(.Item.Count - 1)
                    .DefaultValue = Nothing
                    .DefaultType = GetType(Nullable(Of Double))
                End With

                If Me.GraphicObject.Calculated = False Then
                    .Item.Add(Me.FlowSheet.GetTranslatedString("Mensagemdeerro"), Me, "ErrorMessage", True, Me.FlowSheet.GetTranslatedString("Miscelnea4"), Me.FlowSheet.GetTranslatedString("Mensagemretornadaqua"), True)
                    With .Item(.Item.Count - 1)
                        .DefaultType = GetType(System.String)
                    End With
                End If



            End With

        End Sub

        Public Overrides Function GetPropertyValue(ByVal prop As String, Optional ByVal su As Interfaces.IUnitsOfMeasure = Nothing) As Object

            If su Is Nothing Then su = New SystemsOfUnits.SI
            Dim cv As New SystemsOfUnits.Converter
            Dim value As Double = 0
            Dim propidx As Integer = Convert.ToInt32(prop.Split("_")(2))

            Select Case propidx

                Case 0
                    'PROP_HT_0	Pressure Drop
                    value = SystemsOfUnits.Converter.ConvertFromSI(su.deltaP, Me.DeltaP.GetValueOrDefault)
                Case 1
                    'PROP_HT_1(Efficiency)
                    value = Me.Eficiencia.GetValueOrDefault
                Case 2
                    'PROP_HT_2	Outlet Temperature
                    value = SystemsOfUnits.Converter.ConvertFromSI(su.temperature, Me.OutletTemperature.GetValueOrDefault)
                Case 3
                    'PROP_HT_3	Heat Added
                    value = SystemsOfUnits.Converter.ConvertFromSI(su.heatflow, Me.DeltaQ.GetValueOrDefault)
                Case 4
                    'PROP_HT_4 Outlet molar vapour fraction
                    value = Me.OutletVaporFraction.GetValueOrDefault
                Case 5
                    'PROP_HT_5 DeltaT
                    value = SystemsOfUnits.Converter.ConvertFromSI(su.deltaT, Me.DeltaT.GetValueOrDefault)
            End Select

            Return value

        End Function

        Public Overloads Overrides Function GetProperties(ByVal proptype As Interfaces.Enums.PropertyType) As String()
            Dim i As Integer = 0
            Dim proplist As New ArrayList
            Select Case proptype
                Case PropertyType.RO
                    For i = 5 To 5
                        proplist.Add("PROP_HT_" + CStr(i))
                    Next
                Case PropertyType.RW
                    For i = 0 To 5
                        proplist.Add("PROP_HT_" + CStr(i))
                    Next
                Case PropertyType.WR
                    For i = 0 To 4
                        proplist.Add("PROP_HT_" + CStr(i))
                    Next
                Case PropertyType.ALL
                    For i = 0 To 5
                        proplist.Add("PROP_HT_" + CStr(i))
                    Next
            End Select
            Return proplist.ToArray(GetType(System.String))
            proplist = Nothing
        End Function

        Public Overrides Function SetPropertyValue(ByVal prop As String, ByVal propval As Object, Optional ByVal su As Interfaces.IUnitsOfMeasure = Nothing) As Boolean
            If su Is Nothing Then su = New SystemsOfUnits.SI
            Dim cv As New SystemsOfUnits.Converter
            Dim propidx As Integer = Convert.ToInt32(prop.Split("_")(2))

            Select Case propidx

                Case 0
                    'PROP_HT_0	Pressure Drop
                    Me.DeltaP = SystemsOfUnits.Converter.ConvertToSI(su.deltaP, propval)
                Case 1
                    'PROP_HT_1(Efficiency)
                    Me.Eficiencia = propval
                Case 2
                    'PROP_HT_2	Outlet Temperature
                    Me.OutletTemperature = SystemsOfUnits.Converter.ConvertToSI(su.temperature, propval)
                Case 3
                    'PROP_HT_3	Heat Added
                    Me.DeltaQ = SystemsOfUnits.Converter.ConvertToSI(su.heatflow, propval)
                Case 4
                    'PROP_HT_3	Outlet molar vapour fraction
                    Me.OutletVaporFraction = propval
            End Select
            Return 1
        End Function

        Public Overrides Function GetPropertyUnit(ByVal prop As String, Optional ByVal su As Interfaces.IUnitsOfMeasure = Nothing) As String
            If su Is Nothing Then su = New SystemsOfUnits.SI
            Dim cv As New SystemsOfUnits.Converter
            Dim value As String = ""
            Dim propidx As Integer = Convert.ToInt32(prop.Split("_")(2))

            Select Case propidx

                Case 0
                    'PROP_HT_0	Pressure Drop
                    value = su.deltaP
                Case 1
                    'PROP_HT_1(Efficiency)
                    value = ""
                Case 2
                    'PROP_HT_2	Outlet Temperature
                    value = su.temperature
                Case 3
                    'PROP_HT_3	Heat Added
                    value = su.heatflow
                Case 4
                    'PROP_HT_4	Outlet vapour fraction
                    value = ""
                Case 5
                    'PROP_HT_5 DeltaT
                    value = su.deltaT

            End Select

            Return value
        End Function
    End Class

End Namespace