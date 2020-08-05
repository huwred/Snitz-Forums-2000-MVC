// /*
// ####################################################################################################################
// ##
// ## TokenMessages
// ##   
// ## Author:		Huw Reddick
// ## Copyright:	Huw Reddick, Snitz Forums
// ## based on code from Snitz Forums 2000 (c) Huw Reddick, Michael Anderson, Pierre Gorissen and Richard Kinser
// ## Created:		17/06/2020
// ## 
// ## The use and distribution terms for this software are covered by the 
// ## Eclipse License 1.0 (http://opensource.org/licenses/eclipse-1.0)
// ## which can be found in the file Eclipse.txt at the root of this distribution.
// ## By using this software in any fashion, you are agreeing to be bound by 
// ## the terms of this license.
// ##
// ## You must not remove this notice, or any other, from this software.  
// ##
// #################################################################################################################### 
// */

namespace Snitz.Base.Models
{
    /// <summary>
    /// Log messages and codes
    /// </summary>
    public static class TokenMessages
    {
        // general
        public const string IDX10000 = "IDX10000: The parameter '{0}' cannot be a 'null' or an empty object.";

        // properties, configuration 
        public const string IDX10101 = "IDX10101: MaximumTokenSizeInBytes must be greater than zero. value: '{0}'";
        public const string IDX10104 = "IDX10104: TokenLifetimeInMinutes must be greater than zero. value: '{0}'";

        // token validation
        public const string IDX10209 = "IDX10209: token has length: '{0}' which is larger than the MaximumTokenSizeInBytes: '{1}'.";
        public const string IDX10230 = "IDX10230: Lifetime validation failed. Delegate returned false, securitytoken: '{0}'.";
        public const string IDX10231 = "IDX10231: Audience validation failed. Delegate returned false, securitytoken: '{0}'.";
        public const string IDX10232 = "IDX10232: IssuerSigningKey validation failed. Delegate returned false, securityKey: '{0}'.";
        public const string IDX10241 = "IDX10241: Security token validated. token: '{0}'.";
        public const string IDX10242 = "IDX10242: Security token: '{0}' has a valid signature.";
        public const string IDX10243 = "IDX10243: Reading issuer signing keys from validaiton parameters.";
        public const string IDX10244 = "IDX10244: Issuer is null or empty. Using runtime default for creating claims '{0}'.";

        // token creation
        public const string IDX10401 = "IDX10401: Expires: '{0}' must be after NotBefore: '{1}'.";

        // signature creation / validation
        public const string IDX10500 = "IDX10500: Signature validation failed. No security keys were provided to validate the signature.";
        public const string IDX10501 = "IDX10501: Signature validation failed. Unable to match 'kid': '{0}', \ntoken: '{1}'.";
        public const string IDX10503 = "IDX10503: Signature validation failed. Keys tried: '{0}'.\nExceptions caught:\n '{1}'.\ntoken: '{2}'.";
        public const string IDX10504 = "IDX10504: Unable to validate signature, token does not have a signature: '{0}'.";
        public const string IDX10505 = "IDX10505: Signature validation failed. The user defined 'Delegate' specified on TokenValidationParameters returned null when validating token: '{0}'.";
        public const string IDX10506 = "IDX10506: Signature validation failed. The user defined 'Delegate' specified on TokenValidationParameters did not return a '{0}', but returned a '{1}' when validating token: '{2}'.";
        public const string IDX10507 = "IDX10507: Signature validation failed. ValidateSignature returned null when validating token: '{0}'.";
        public const string IDX10508 = "IDX10508: Signing JWT is not supported for: Algorithm: '{0}', SecurityKey: '{1}'.";

        // encryption / decryption
        public const string IDX10600 = "IDX10600: Decryption failed. There are no security keys for decryption.";
        public const string IDX10601 = "IDX10601: Decryption failed. Unable to match 'kid': '{0}', \ntoken: '{1}'.";
        public const string IDX10603 = "IDX10603: Decryption failed. Keys tried: '{0}'.\nExceptions caught:\n '{1}'.\ntoken: '{2}'";
        public const string IDX10604 = "IDX10604: Decryption failed. Exception: '{0}'.";
        public const string IDX10605 = "IDX10605: Decryption failed. Only 'dir' is currently supported. JWE alg is: '{0}'.";
        public const string IDX10606 = "IDX10606: Decryption failed. To decrypt a JWE there must be 5 parts. 'tokenParts' is of length: '{0}'.";
        public const string IDX10607 = "IDX10607: Decryption skipping key: '{0}', both validationParameters.CryptoProviderFactory and key.CryptoProviderFactory are null.";
        public const string IDX10608 = "IDX10608: Decryption skipping key: '{0}', it is not a '{1}'.";
        public const string IDX10609 = "IDX10609: Decryption failed. No Keys tried: token: '{0}'.";
        public const string IDX10610 = "IDX10610: Decryption failed. Could not create decryption provider. Key: '{0}', Algorithm: '{1}'.";
        public const string IDX10611 = "IDX10611: Decryption failed. Encryption is not supported for: Algorithm: '{0}', SecurityKey: '{1}'.";
        public const string IDX10612 = "IDX10612: Decryption failed. Header.Enc is null or empty, it must be specified.";
        public const string IDX10613 = "IDX10613: Decryption failed. JwtHeader (tokenParts[0]) is null or empty.";
        public const string IDX10614 = "IDX10614: Decryption failed. JwtHeader.Base64UrlDeserialize(tokenParts[0]): '{0}'. Inner exception: '{1}'.";
        public const string IDX10615 = "IDX10615: Encryption failed. No support for: Algorithm: '{0}', SecurityKey: '{1}'.";
        public const string IDX10616 = "IDX10616: Encryption failed. EncryptionProvider failed for: Algorithm: '{0}', SecurityKey: '{1}'. See inner exception.";
        public const string IDX10617 = "IDX10617: Encryption failed. Keywrap is only supported for: '{0}', '{1}' and '{2}'. The content encryption specified is: '{3}'.";

        // crypto errors
        public const string IDX10635 = "IDX10635: Unable to create signature. '{0}' returned a null '{1}'. SecurityKey: '{2}', Algorithm: '{3}'";
        public const string IDX10636 = "IDX10636: CryptoProviderFactory.CreateForVerifying returned null for key: '{0}', signatureAlgorithm: '{1}'.";
        public const string IDX10644 = "IDX10644: Creating raw signature using the signature provider.";
        public const string IDX10645 = "IDX10645: Creating raw signature using the signature credentials.";
        public const string IDX10646 = "IDX10646: CryptoProviderFactory.CreateForSigning returned null for key: '{0}', signatureAlgorithm: '{1}'.";


        // JWT specific errors
        public const string IDX10700 = "IDX10700: Error found while parsing date time. The '{0}' claim has value '{1}' which is could not be parsed to an integer.";
        public const string IDX10701 = "IDX10701: Error found while parsing date time. The '{0}' claim has value '{1}' does not lie in the valid range.";
        public const string IDX10706 = "IDX10706: '{0}' can only write SecurityTokens of type: '{1}', 'token' type is: '{2}'.";
        public const string IDX10709 = "IDX10709: JWT is not well formed: '{0}'.\nThe token needs to be in JWS or JWE Compact Serialization Format. (JWS): 'EncodedHeader.EndcodedPayload.EncodedSignature'. (JWE): 'EncodedProtectedHeader.EncodedEncryptedKey.EncodedInitializationVector.EncodedCiphertext.EncodedAuthenticationTag'.";
        public const string IDX10710 = "IDX10710: Only a single 'Actor' is supported. Found second claim of type: '{0}', value: '{1}'";
        public const string IDX10711 = "IDX10711: actor.BootstrapContext is not a string AND actor.BootstrapContext is not a JWT";
        public const string IDX10712 = "IDX10712: actor.BootstrapContext is null. Creating the token using actor.Claims.";
        public const string IDX10713 = "IDX10713: Creating actor value using actor.BootstrapContext(as string)";
        public const string IDX10714 = "IDX10714: Creating actor value using actor.BootstrapContext.rawData";
        public const string IDX10715 = "IDX10715: Creating actor value by writing the JwtSecurityToken created from actor.BootstrapContext";
        public const string IDX10716 = "IDX10716: Decoding token: '{0}' into header, payload and signature.";
        public const string IDX10717 = "IDX10717: Deserializing header: '{0}' from the token.";
        public const string IDX10718 = "IDX10718: Deserializing payload: '{0}' from the token.";
        public const string IDX10720 = "IDX10720: Token string does not match the token formats: JWE (header.encryptedKey.iv.ciphertext.tag) or JWS (header.payload.signature)";
        public const string IDX10721 = "IDX10721: Creating JwtSecurityToken: Issuer: '{0}', Audience: '{1}'";
        public const string IDX10722 = "IDX10722: Creating security token from the header: '{0}', payload: '{1}' and raw signature: '{2}'.";
        public const string IDX10723 = "IDX10723: Unable to decode the payload '{0}' as Base64Url encoded string. jwtEncodedString: '{1}'.";
        public const string IDX10724 = "IDX10724: Unable to decode the signature '{0}' as Base64Url encoded string. jwtEncodedString: '{1}'.";
        public const string IDX10725 = "IDX10725: Unable to decode the encrypted key '{0}' as Base64Url encoded string. jwtEncodedString: '{1}'.";
        public const string IDX10726 = "IDX10726: Unable to decode the initial vector '{0}' as Base64Url encoded string. jwtEncodedString: '{1}'.";
        public const string IDX10727 = "IDX10727: Unable to decode the cyphertext '{0}' as Base64Url encoded string. jwtEncodedString: '{1}'.";
        public const string IDX10728 = "IDX10728: Unable to decode the authentication tag '{0}' as Base64Url encoded string. jwtEncodedString: '{1}'.";
        public const string IDX10729 = "IDX10729: Unable to decode the header '{0}' as Base64Url encoded string. jwtEncodedString: '{1}'.";
        public const string IDX10730 = "IDX10730: Failed to create the token encryption provider.";
        public const string IDX10731 = "IDX10731: The resolved key for direct use is not a symmetric key.";
        public const string IDX10733 = "IDX10733: Unable to obtain a CryptoProviderFactory, both EncryptingCredentials.CryptoProviderFactory and EncryptingCredentials.Key.CrypoProviderFactory are both null.";
        public const string IDX10734 = "IDX10734: Only 'dir' is supported.";
        public const string IDX10735 = "IDX10735: If JwtSecurityToken.InnerToken != null, then JwtSecurityToken.Header.EncryptingCredentials must be set.";
        public const string IDX10736 = "IDX10736: JwtSecurityToken.SigningCredentials is not supported when JwtSecurityToken.InnerToken is set.";
        public const string IDX10737 = "IDX10737: EncryptingCredentials set on JwtSecurityToken.InnerToken is not supported.";
        public const string IDX10738 = "IDX10738: Header.Cty != null, assuming JWS. Cty: '{0}'.";

    }

}
