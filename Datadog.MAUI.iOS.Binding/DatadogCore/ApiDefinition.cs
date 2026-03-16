using System;
using Foundation;
using ObjCRuntime;
using UIKit;
using Datadog.iOS.DatadogInternal;

namespace Datadog.iOS.DatadogCore
{
	// @interface DDCrossPlatformExtension : NSObject
	[BaseType (typeof(NSObject))]
	interface DDCrossPlatformExtension
	{
		// +(void)subscribeToSharedContext:(void (^ _Nonnull)(DDSharedContext * _Nullable))toSharedContext;
		[Static]
		[Export ("subscribeToSharedContext:")]
		void SubscribeToSharedContext (Action<DDSharedContext> toSharedContext);

		// +(void)unsubscribeFromSharedContext;
		[Static]
		[Export ("unsubscribeFromSharedContext")]
		void UnsubscribeFromSharedContext ();
	}

	// @interface DDSharedContext : NSObject
	[BaseType (typeof(NSObject))]
	[DisableDefaultCtor]
	interface DDSharedContext
	{
		// @property (readonly, copy, nonatomic) NSString * _Nullable userId;
		[NullAllowed, Export ("userId")]
		string UserId { get; }

		// @property (readonly, copy, nonatomic) NSString * _Nullable accountId;
		[NullAllowed, Export ("accountId")]
		string AccountId { get; }
	}

	// @protocol DDDataEncryption
	[Protocol]
	[BaseType(typeof(NSObject))]
	interface DDDataEncryption
	{
		// @required -(NSData * _Nullable)encryptWithData:(NSData * _Nonnull)data error:(NSError * _Nullable * _Nullable)error __attribute__((warn_unused_result("")));
		[Abstract]
		[Export ("encryptWithData:error:")]
		[return: NullAllowed]
		NSData EncryptWithData (NSData data, [NullAllowed] out NSError error);

		// @required -(NSData * _Nullable)decryptWithData:(NSData * _Nonnull)data error:(NSError * _Nullable * _Nullable)error __attribute__((warn_unused_result("")));
		[Abstract]
		[Export ("decryptWithData:error:")]
		[return: NullAllowed]
		NSData DecryptWithData (NSData data, [NullAllowed] out NSError error);
	}

	// @protocol DDServerDateProvider
	[Protocol]
	[BaseType(typeof(NSObject))]
	interface DDServerDateProvider
	{
		// @required -(void)synchronizeWithUpdate:(void (^ _Nonnull)(NSTimeInterval))update;
		[Abstract]
		[Export ("synchronizeWithUpdate:")]
		void SynchronizeWithUpdate (Action<double> update);
	}

	//
	// NOTE: DDConfiguration, DDSite, DDTrackingConsent, DDDatadog,
	// DDURLSessionInstrumentation, DDURLSessionInstrumentationConfiguration, and
	// DDURLSessionInstrumentationFirstPartyHostsTracing have been removed.
	//
	// These are Swift value types compiled with library evolution, so they live in
	// __objc_stublist (not __objc_classlist). The static registrar would generate
	// _OBJC_CLASS_$_DD* references that dyld cannot satisfy at launch, causing a
	// "symbol not found in flat namespace" crash. Since all initialization goes
	// through DatadogWrapper (DDWrapperCore, DDWrapperRUM, etc.), these bindings
	// are not needed.
	//
}
