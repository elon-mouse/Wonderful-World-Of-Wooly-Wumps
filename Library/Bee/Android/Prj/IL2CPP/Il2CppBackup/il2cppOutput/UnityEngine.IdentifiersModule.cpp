#include "pch-cpp.hpp"






struct String_t;
struct Void_t4861ACF8F4594C3437BB48B6E56783494B843915;



IL2CPP_EXTERN_C_BEGIN
IL2CPP_EXTERN_C_END

#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif
struct U3CModuleU3E_tD52CF2B8C4BFE8E85EB31077ED0468E46BD9D682 
{
};
struct Identifiers_tC0FC1D5B3C7872A17DFBB0C4413845738B1A4FA1  : public RuntimeObject
{
};
struct String_t  : public RuntimeObject
{
	int32_t ____stringLength;
	Il2CppChar ____firstChar;
};
struct ValueType_t6D9B272BD21782F0A9A14F2E41F85A50E97A986F  : public RuntimeObject
{
};
struct ValueType_t6D9B272BD21782F0A9A14F2E41F85A50E97A986F_marshaled_pinvoke
{
};
struct ValueType_t6D9B272BD21782F0A9A14F2E41F85A50E97A986F_marshaled_com
{
};
struct ManagedSpanWrapper_tE7FC4BBB631B130757F8DEB15853D98FD3D5DC0E 
{
	void* ___begin;
	int32_t ___length;
};
struct Void_t4861ACF8F4594C3437BB48B6E56783494B843915 
{
	union
	{
		struct
		{
		};
		uint8_t Void_t4861ACF8F4594C3437BB48B6E56783494B843915__padding[1];
	};
};
struct String_t_StaticFields
{
	String_t* ___Empty;
};
#ifdef __clang__
#pragma clang diagnostic pop
#endif



IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR String_t* Identifiers_GetInstallationId_mE83CB40B3E23BE88A50981DF00B8806D654D0A81 (const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR String_t* OutStringMarshaller_GetStringAndDispose_mB15D41A9893BBC55074D4910259FA722129DB062 (ManagedSpanWrapper_tE7FC4BBB631B130757F8DEB15853D98FD3D5DC0E ___0_managedSpan, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Identifiers_GetInstallationId_Injected_mE29A1BB73C1587EACFF0A6104DD068DB4FA711CC (ManagedSpanWrapper_tE7FC4BBB631B130757F8DEB15853D98FD3D5DC0E* ___0_ret, const RuntimeMethod* method) ;
#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif
#ifdef __clang__
#pragma clang diagnostic pop
#endif
#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif
// Method Definition Index: 98639
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR String_t* Identifiers_get_installationId_mD3ECC6BF716CA29158D977D82861214A105AA6B9 (const RuntimeMethod* method) 
{
	{
		String_t* L_0;
		L_0 = Identifiers_GetInstallationId_mE83CB40B3E23BE88A50981DF00B8806D654D0A81(NULL);
		return L_0;
	}
}
// Method Definition Index: 98640
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR String_t* Identifiers_GetInstallationId_mE83CB40B3E23BE88A50981DF00B8806D654D0A81 (const RuntimeMethod* method) 
{
	ManagedSpanWrapper_tE7FC4BBB631B130757F8DEB15853D98FD3D5DC0E V_0;
	memset((&V_0), 0, sizeof(V_0));
	String_t* V_1 = NULL;
	{
		auto __finallyBlock = il2cpp::utils::Finally([&]
		{

FINALLY_0009:
			{
				ManagedSpanWrapper_tE7FC4BBB631B130757F8DEB15853D98FD3D5DC0E L_0 = V_0;
				String_t* L_1;
				L_1 = OutStringMarshaller_GetStringAndDispose_mB15D41A9893BBC55074D4910259FA722129DB062(L_0, NULL);
				V_1 = L_1;
				return;
			}
		});
		try
		{
			Identifiers_GetInstallationId_Injected_mE29A1BB73C1587EACFF0A6104DD068DB4FA711CC((&V_0), NULL);
			goto IL_0011;
		}
		catch(Il2CppExceptionWrapper& e)
		{
			__finallyBlock.StoreException(e.ex);
		}
	}

IL_0011:
	{
		String_t* L_2 = V_1;
		return L_2;
	}
}
// Method Definition Index: 98641
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Identifiers_GetInstallationId_Injected_mE29A1BB73C1587EACFF0A6104DD068DB4FA711CC (ManagedSpanWrapper_tE7FC4BBB631B130757F8DEB15853D98FD3D5DC0E* ___0_ret, const RuntimeMethod* method) 
{
	typedef void (*Identifiers_GetInstallationId_Injected_mE29A1BB73C1587EACFF0A6104DD068DB4FA711CC_ftn) (ManagedSpanWrapper_tE7FC4BBB631B130757F8DEB15853D98FD3D5DC0E*);
	static Identifiers_GetInstallationId_Injected_mE29A1BB73C1587EACFF0A6104DD068DB4FA711CC_ftn _il2cpp_icall_func;
	if (!_il2cpp_icall_func)
	_il2cpp_icall_func = (Identifiers_GetInstallationId_Injected_mE29A1BB73C1587EACFF0A6104DD068DB4FA711CC_ftn)il2cpp_codegen_resolve_icall ("UnityEngine.Identifiers.Identifiers::GetInstallationId_Injected(UnityEngine.Bindings.ManagedSpanWrapper&)");
	_il2cpp_icall_func(___0_ret);
}
#ifdef __clang__
#pragma clang diagnostic pop
#endif
