import 'package:flutter/cupertino.dart' show CupertinoPageTransitionsBuilder;
import 'package:flutter/material.dart';

import 'ida_colors.dart';
import 'ida_palette.dart';


abstract final class IdaSpace {
  static const s1 = 4.0;
  static const s2 = 8.0;
  static const s3 = 12.0;
  static const s4 = 16.0;
  static const s5 = 20.0;
  static const s6 = 24.0;
  static const s8 = 32.0;
  static const s10 = 40.0;
  static const s12 = 48.0;
  static const s16 = 64.0;
}


abstract final class IdaRadius {
  static const sm = 6.0;
  static const md = 8.0;
  static const lg = 12.0;
  static const xl = 16.0;
  static const xxl = 20.0;
  static const full = 9999.0;

  static const smR = BorderRadius.all(Radius.circular(sm));
  static const mdR = BorderRadius.all(Radius.circular(md));
  static const lgR = BorderRadius.all(Radius.circular(lg));
  static const xlR = BorderRadius.all(Radius.circular(xl));
  static const xxlR = BorderRadius.all(Radius.circular(xxl));
  static const fullR = BorderRadius.all(Radius.circular(full));


  static const sheetR = BorderRadius.vertical(top: Radius.circular(xxl));
}


abstract final class IdaMotion {
  static const fast = Duration(milliseconds: 120);
  static const base = Duration(milliseconds: 200);
  static const slow = Duration(milliseconds: 320);
  static const ease = Cubic(0.2, 0, 0.2, 1);


  static Duration of(BuildContext context, Duration d) =>
      MediaQuery.maybeDisableAnimationsOf(context) ?? false ? Duration.zero : d;
}


abstract final class IdaSizes {
  static const appBarHeight = 56.0;
  static const bottomNavHeight = 64.0;
  static const controlHeight = 44.0;
  static const controlHeightLg = 48.0;


  static const minTapTarget = 44.0;
  static const screenPadding = EdgeInsets.all(IdaSpace.s4);


  static const iconXs = 14.0;
  static const iconSm = 16.0;
  static const iconMd = 20.0;
  static const iconLg = 24.0;
  static const iconXl = 28.0;

  static const avatarSm = 36.0;
  static const avatarMd = 48.0;
  static const avatarLg = 72.0;


  static const emptyCircle = 64.0;
  static const tileIcon = 44.0;

  static const keypadKey = 72.0;
  static const pinDot = 14.0;
  static const otpCellWidth = 46.0;
  static const otpCellHeight = 56.0;

  static const chipHeight = 36.0;


  static const logoLockup = 40.0;
  static const logoMark = 32.0;
  static const brandBar = 3.0;
  static const brandTick = 48.0;
  static const selectedBar = 3.0;


  static const maxContentWidth = 560.0;
}


abstract final class IdaFonts {
  static const String base = 'NotoSansThai';
  static const List<String> baseFallback = ['NotoSans'];
  static const String display = 'Poppins';


  static const String numeric = 'NotoSans';
  static const List<String> numericFallback = ['NotoSansThai'];


  static const tabular = <FontFeature>[FontFeature.tabularFigures()];
}


TextTheme _textTheme(Color onSurface, Color onSurfaceVariant) => TextTheme(
      displaySmall: TextStyle(
        fontSize: 32,
        height: 40 / 32,
        fontWeight: FontWeight.w700,
        color: onSurface,
      ),
      headlineSmall: TextStyle(
        fontSize: 24,
        height: 34 / 24,
        fontWeight: FontWeight.w700,
        color: onSurface,
      ),
      titleLarge: TextStyle(
        fontSize: 20,
        height: 28 / 20,
        fontWeight: FontWeight.w600,
        color: onSurface,
      ),
      titleMedium: TextStyle(
        fontSize: 18,
        height: 26 / 18,
        fontWeight: FontWeight.w600,
        color: onSurface,
      ),
      titleSmall: TextStyle(
        fontSize: 16,
        height: 24 / 16,
        fontWeight: FontWeight.w600,
        color: onSurface,
      ),
      bodyLarge: TextStyle(fontSize: 16, height: 26 / 16, color: onSurface),
      bodyMedium: TextStyle(fontSize: 14, height: 22 / 14, color: onSurface),
      bodySmall: TextStyle(fontSize: 12, height: 18 / 12, color: onSurfaceVariant),
      labelLarge: TextStyle(
        fontSize: 14,
        height: 20 / 14,
        fontWeight: FontWeight.w600,
        color: onSurface,
      ),
      labelMedium: TextStyle(
        fontSize: 12,
        height: 18 / 12,
        fontWeight: FontWeight.w600,
        color: onSurfaceVariant,
      ),
      labelSmall: TextStyle(
        fontSize: 11,
        height: 16 / 11,
        fontWeight: FontWeight.w600,
        color: onSurfaceVariant,
      ),
    );


TextStyle idaAmountStyle(BuildContext context, {double? value, TextStyle? base}) {
  final style = base ?? Theme.of(context).textTheme.bodyMedium!;
  final p = context.ida;
  final color = switch (value) {
    final v? when v > 0 => p.successText,
    final v? when v < 0 => p.dangerText,
    _ => style.color,
  };
  return idaNumeric(style).copyWith(color: color);
}


TextStyle idaNumeric(TextStyle style) => style.copyWith(
      fontFamily: IdaFonts.numeric,
      fontFamilyFallback: IdaFonts.numericFallback,
      fontFeatures: IdaFonts.tabular,
    );

ThemeData _buildTheme(IdaPalette p, Brightness brightness) {
  final text = _textTheme(p.text, p.textSecondary);
  const labelStyle = TextStyle(fontSize: 14, fontWeight: FontWeight.w600);

  OutlineInputBorder border(Color c, [double w = 1]) => OutlineInputBorder(
        borderRadius: IdaRadius.mdR,
        borderSide: BorderSide(color: c, width: w),
      );

  return ThemeData(
    useMaterial3: true,
    brightness: brightness,
    fontFamily: IdaFonts.base,
    fontFamilyFallback: IdaFonts.baseFallback,
    scaffoldBackgroundColor: p.bg,
    extensions: [p],
    colorScheme: ColorScheme(
      brightness: brightness,
      primary: p.primary,
      onPrimary: p.onPrimary,
      primaryContainer: p.primarySoft,
      onPrimaryContainer: p.primaryText,
      secondary: p.accent,
      onSecondary: IdaColors.neutral900,
      secondaryContainer: p.accentSoft,
      onSecondaryContainer: p.accentStrong,
      tertiary: p.success,
      onTertiary: p.onPrimary,
      tertiaryContainer: p.successSoft,
      onTertiaryContainer: p.successText,
      error: p.danger,
      onError: p.onPrimary,
      errorContainer: p.dangerSoft,
      onErrorContainer: p.dangerText,
      surface: p.surface,
      onSurface: p.text,
      surfaceContainerLowest: p.surface,
      surfaceContainerLow: p.surface,
      surfaceContainer: p.surface,
      surfaceContainerHigh: p.surface,
      surfaceContainerHighest: p.surface2,
      onSurfaceVariant: p.textSecondary,
      outline: p.borderStrong,
      outlineVariant: p.border,
      scrim: p.overlay,
      shadow: IdaColors.blue900,
    ),
    textTheme: text,
    iconTheme: IconThemeData(color: p.textSecondary, size: IdaSizes.iconLg),

    appBarTheme: AppBarTheme(
      backgroundColor: p.primary,
      foregroundColor: p.onPrimary,
      surfaceTintColor: Colors.transparent,
      elevation: 0,
      scrolledUnderElevation: 0,
      centerTitle: true,
      toolbarHeight: IdaSizes.appBarHeight,
      titleTextStyle: text.titleMedium!.copyWith(color: p.onPrimary),
      iconTheme: IconThemeData(color: p.onPrimary, size: IdaSizes.iconLg),
      actionsIconTheme: IconThemeData(color: p.onPrimary, size: IdaSizes.iconLg),
    ),
    cardTheme: CardThemeData(
      color: p.surface,
      surfaceTintColor: Colors.transparent,
      elevation: 0,
      margin: EdgeInsets.zero,
      shape: const RoundedRectangleBorder(borderRadius: IdaRadius.xlR),
    ),
    dividerTheme: DividerThemeData(color: p.border, thickness: 1, space: 1),

    inputDecorationTheme: InputDecorationTheme(
      filled: true,
      fillColor: p.surface,
      isDense: false,
      contentPadding: const EdgeInsets.symmetric(
        horizontal: IdaSpace.s4,
        vertical: IdaSpace.s3,
      ),
      hintStyle: text.bodyLarge!.copyWith(color: p.textMuted),
      labelStyle: text.labelLarge!.copyWith(color: p.textSecondary),
      floatingLabelBehavior: FloatingLabelBehavior.never,
      helperStyle: text.bodySmall,
      errorStyle: text.bodySmall!.copyWith(color: p.dangerText),
      errorMaxLines: 3,
      prefixIconColor: p.textSecondary,
      suffixIconColor: p.textSecondary,
      border: border(p.borderStrong),
      enabledBorder: border(p.borderStrong),
      disabledBorder: border(p.border),
      focusedBorder: border(p.accent, 2),
      errorBorder: border(p.danger),
      focusedErrorBorder: border(p.danger, 2),
    ),
    elevatedButtonTheme: ElevatedButtonThemeData(
      style: ElevatedButton.styleFrom(
        backgroundColor: p.primary,
        foregroundColor: p.onPrimary,
        disabledBackgroundColor: p.skeleton,
        disabledForegroundColor: p.textMuted,
        minimumSize: const Size(IdaSizes.minTapTarget, IdaSizes.controlHeightLg),
        padding: const EdgeInsets.symmetric(horizontal: IdaSpace.s5),
        shape: const RoundedRectangleBorder(borderRadius: IdaRadius.mdR),
        elevation: 0,
        textStyle: labelStyle,
      ),
    ),
    filledButtonTheme: FilledButtonThemeData(
      style: FilledButton.styleFrom(
        backgroundColor: p.primary,
        foregroundColor: p.onPrimary,
        disabledBackgroundColor: p.skeleton,
        disabledForegroundColor: p.textMuted,
        minimumSize: const Size(IdaSizes.minTapTarget, IdaSizes.controlHeightLg),
        padding: const EdgeInsets.symmetric(horizontal: IdaSpace.s5),
        shape: const RoundedRectangleBorder(borderRadius: IdaRadius.mdR),
        textStyle: labelStyle,
      ),
    ),
    outlinedButtonTheme: OutlinedButtonThemeData(
      style: OutlinedButton.styleFrom(
        foregroundColor: p.primaryText,
        minimumSize: const Size(IdaSizes.minTapTarget, IdaSizes.controlHeightLg),
        padding: const EdgeInsets.symmetric(horizontal: IdaSpace.s5),
        shape: const RoundedRectangleBorder(borderRadius: IdaRadius.mdR),
        textStyle: labelStyle,
      ).copyWith(

        side: WidgetStateProperty.resolveWith(
          (s) => BorderSide(color: s.contains(WidgetState.disabled) ? p.border : p.primaryText, width: 1.5),
        ),
      ),
    ),
    textButtonTheme: TextButtonThemeData(
      style: TextButton.styleFrom(
        foregroundColor: p.primaryText,
        minimumSize: const Size(IdaSizes.minTapTarget, IdaSizes.controlHeight),
        padding: const EdgeInsets.symmetric(horizontal: IdaSpace.s3),
        shape: const RoundedRectangleBorder(borderRadius: IdaRadius.mdR),
        textStyle: labelStyle,
      ),
    ),
    iconButtonTheme: IconButtonThemeData(
      style: IconButton.styleFrom(
        minimumSize: const Size(IdaSizes.minTapTarget, IdaSizes.minTapTarget),
      ),
    ),
    checkboxTheme: CheckboxThemeData(
      shape: const RoundedRectangleBorder(borderRadius: IdaRadius.smR),
      side: BorderSide(color: p.borderStrong, width: 1.5),
      fillColor: WidgetStateProperty.resolveWith(
        (s) => s.contains(WidgetState.selected) ? p.primary : Colors.transparent,
      ),
      checkColor: WidgetStatePropertyAll(p.onPrimary),
    ),
    switchTheme: SwitchThemeData(
      thumbColor: WidgetStateProperty.resolveWith(
        (s) => s.contains(WidgetState.selected) ? p.onPrimary : p.borderStrong,
      ),
      trackColor: WidgetStateProperty.resolveWith(
        (s) => s.contains(WidgetState.selected) ? p.primary : p.surface2,
      ),
      trackOutlineColor: WidgetStateProperty.resolveWith(
        (s) => s.contains(WidgetState.selected) ? p.primary : p.borderStrong,
      ),
    ),
    tabBarTheme: TabBarThemeData(
      labelColor: p.primaryText,
      unselectedLabelColor: p.textSecondary,
      indicatorColor: p.primaryText,
      indicatorSize: TabBarIndicatorSize.label,
      dividerColor: p.border,
      labelStyle: labelStyle,
      unselectedLabelStyle: labelStyle.copyWith(fontWeight: FontWeight.w500),
    ),
    chipTheme: ChipThemeData(
      backgroundColor: p.surface,
      selectedColor: p.accentSoft,
      labelStyle: text.labelLarge!.copyWith(color: p.textSecondary),
      secondaryLabelStyle: text.labelLarge!.copyWith(color: p.accentStrong),
      side: BorderSide(color: p.border),
      shape: const StadiumBorder(),
      showCheckmark: false,
      padding: const EdgeInsets.symmetric(horizontal: IdaSpace.s2),
    ),

    navigationBarTheme: NavigationBarThemeData(
      backgroundColor: p.surface,
      surfaceTintColor: Colors.transparent,
      height: IdaSizes.bottomNavHeight,
      indicatorColor: p.primarySoft,
      elevation: 0,
      labelBehavior: NavigationDestinationLabelBehavior.alwaysShow,
      labelTextStyle: WidgetStateProperty.resolveWith(
        (s) => text.labelMedium!.copyWith(
          color: s.contains(WidgetState.selected) ? p.primaryText : p.textSecondary,
          fontWeight: s.contains(WidgetState.selected) ? FontWeight.w700 : FontWeight.w500,
        ),
      ),
      iconTheme: WidgetStateProperty.resolveWith(
        (s) => IconThemeData(
          size: IdaSizes.iconLg,
          color: s.contains(WidgetState.selected) ? p.primaryText : p.borderStrong,
        ),
      ),
    ),
    bottomSheetTheme: BottomSheetThemeData(
      backgroundColor: p.surface,
      surfaceTintColor: Colors.transparent,
      modalBackgroundColor: p.surface,
      showDragHandle: true,
      dragHandleColor: p.dividerStrong,
      shape: const RoundedRectangleBorder(borderRadius: IdaRadius.sheetR),
      clipBehavior: Clip.antiAlias,
    ),
    dialogTheme: DialogThemeData(
      backgroundColor: p.surface,
      surfaceTintColor: Colors.transparent,
      shape: const RoundedRectangleBorder(borderRadius: IdaRadius.xlR),
      titleTextStyle: text.titleMedium,
      contentTextStyle: text.bodyMedium!.copyWith(color: p.textSecondary),
      barrierColor: p.overlay,
    ),
    snackBarTheme: SnackBarThemeData(
      backgroundColor: brightness == Brightness.dark ? p.surface2 : IdaColors.neutral900,
      contentTextStyle: text.bodyMedium!.copyWith(color: IdaColors.textInverse),
      behavior: SnackBarBehavior.floating,
      shape: const RoundedRectangleBorder(borderRadius: IdaRadius.lgR),
      insetPadding: const EdgeInsets.fromLTRB(IdaSpace.s4, 0, IdaSpace.s4, IdaSpace.s4),
    ),
    progressIndicatorTheme: ProgressIndicatorThemeData(
      color: p.accent,
      linearTrackColor: p.skeleton,
      circularTrackColor: Colors.transparent,
    ),
    listTileTheme: ListTileThemeData(
      iconColor: p.textSecondary,
      textColor: p.text,
      minVerticalPadding: IdaSpace.s3,
      contentPadding: const EdgeInsets.symmetric(horizontal: IdaSpace.s4),
    ),
    pageTransitionsTheme: const PageTransitionsTheme(
      builders: {
        TargetPlatform.android: FadeForwardsPageTransitionsBuilder(),
        TargetPlatform.iOS: CupertinoPageTransitionsBuilder(),
      },
    ),
    splashFactory: InkSparkle.splashFactory,
  );
}


final ThemeData idaLightTheme = _buildTheme(IdaPalette.light, Brightness.light);


final ThemeData idaDarkTheme = _buildTheme(IdaPalette.dark, Brightness.dark);
