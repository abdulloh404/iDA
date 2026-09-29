import 'package:flutter/material.dart';


abstract final class IdaColors {

  static const blue900 = Color(0xFF002952);
  static const blue800 = Color(0xFF00509E);
  static const blue700 = Color(0xFF056ACC);
  static const blue600 = Color(0xFF006FDB);
  static const blue500 = Color(0xFF1589F9);
  static const blue400 = Color(0xFF479DF0);
  static const blue200 = Color(0xFF8ABBEA);

  static const cyan800 = Color(0xFF006B93);
  static const cyan600 = Color(0xFF00A3E0);
  static const cyan500 = Color(0xFF1ABCF9);
  static const cyan400 = Color(0xFF1EC2FF);
  static const cyan300 = Color(0xFF56CEFB);
  static const cyan200 = Color(0xFF84D6F5);
  static const cyan100 = Color(0xFFC2E7F4);

  static const green700 = Color(0xFF299A50);
  static const green500 = Color(0xFF48C774);
  static const green400 = Color(0xFF74CE93);
  static const green300 = Color(0xFF72DA96);
  static const green200 = Color(0xFFA1DEB6);
  static const green100 = Color(0xFFC4E4CF);
  static const green50 = Color(0xFFEFF5F1);


  static const green800 = Color(0xFF1F7A3E);

  static const neutral0 = Color(0xFFFFFFFF);
  static const neutral25 = Color(0xFFF5F8FB);
  static const neutral50 = Color(0xFFF1F5F9);
  static const neutral100 = Color(0xFFE4EBF2);
  static const neutral200 = Color(0xFFCBD6E2);
  static const neutral300 = Color(0xFFA7B6C6);
  static const neutral400 = Color(0xFF7C8FA3);
  static const neutral450 = Color(0xFF64788D);
  static const neutral500 = Color(0xFF5C7086);
  static const neutral600 = Color(0xFF46586B);
  static const neutral700 = Color(0xFF33455A);
  static const neutral900 = Color(0xFF002952);

  static const danger700 = Color(0xFFB42318);
  static const danger500 = Color(0xFFD32F2F);
  static const danger50 = Color(0xFFFDECEA);
  static const warning700 = Color(0xFFB54708);
  static const warning500 = Color(0xFFF79009);
  static const warning50 = Color(0xFFFEF4E6);


  static const primary = blue800;
  static const primaryHover = blue700;
  static const primaryActive = blue900;

  static const primarySoft = Color(0xFFE2EEFA);
  static const onPrimary = Color(0xFFFFFFFF);

  static const accent = cyan600;
  static const accentStrong = cyan800;
  static const accentSoft = cyan100;

  static const success = green700;
  static const successText = green800;
  static const successSoft = green50;

  static const danger = danger500;
  static const dangerText = danger700;
  static const dangerSoft = danger50;

  static const warning = warning500;
  static const warningText = warning700;
  static const warningSoft = warning50;

  static const bg = neutral25;
  static const surface = neutral0;
  static const surface2 = neutral50;


  static const border = neutral100;

  static const borderStrong = neutral400;

  static const text = neutral900;
  static const textSecondary = neutral500;

  static const textMuted = neutral450;
  static const textInverse = Color(0xFFFFFFFF);
  static const link = cyan800;

  static const scrollbarThumb = neutral400;
  static const scrollbarThumbHover = neutral450;

  static const focusRing = Color(0x5900A3E0);
  static const overlay = Color(0x73002952);


  static const heroOverlay = Color(0x59002952);


  static const gradientBrand = LinearGradient(
    begin: Alignment.centerLeft,
    end: Alignment.centerRight,
    colors: [blue800, cyan600, green500],
    stops: [0.0, 0.55, 1.0],
  );


  static const gradientHero = LinearGradient(
    begin: Alignment.topLeft,
    end: Alignment.bottomRight,
    colors: [green500, cyan600, blue800],
    stops: [0.0, 0.45, 1.0],
  );


  static const gradientPrimaryButton = LinearGradient(
    begin: Alignment.topCenter,
    end: Alignment.bottomCenter,
    colors: [blue800, blue700],
  );


  static const gradientCardAccent = LinearGradient(
    begin: Alignment.topLeft,
    end: Alignment.bottomRight,
    colors: [blue800, cyan600],
  );


  static const darkBg = Color(0xFF0B1B2B);
  static const darkSurface = Color(0xFF10263A);
  static const darkSurface2 = Color(0xFF173049);
  static const darkBorder = Color(0xFF22405C);
  static const darkBorderStrong = Color(0xFF2E5273);
  static const darkText = Color(0xFFEAF2F9);
  static const darkTextSecondary = Color(0xFF9DB2C7);

  static const darkTextMuted = Color(0xFF8FA1B5);


  static const darkPrimary = blue600;
  static const darkPrimaryHover = blue700;
  static const darkPrimaryActive = blue800;

  static const darkPrimaryText = blue400;

  static const darkPrimarySoft = Color(0xFF0C3153);

  static const darkAccent = cyan400;
  static const darkAccentStrong = cyan300;
  static const darkAccentSoft = Color(0xFF123A52);
  static const darkLink = cyan300;

  static const darkSuccessText = green400;
  static const darkSuccessSoft = Color(0x80143321);
  static const darkDangerText = Color(0xFFF5837C);
  static const darkDangerSoft = Color(0xFF3A1C1A);
  static const darkWarningText = Color(0xFFF2B450);
  static const darkWarningSoft = Color(0xFF3A2A14);


  static const chartSeriesLight = <Color>[
    Color(0xFF00509E),
    Color(0xFFEB6834),
    Color(0xFF00A3E0),
    Color(0xFFEDA100),
    Color(0xFFE87BA4),
    Color(0xFF299A50),
    Color(0xFF4A3AA7),
    Color(0xFFE34948),
  ];

  static const chartSeriesDark = <Color>[
    Color(0xFF1589F9),
    Color(0xFFD95926),
    Color(0xFF0098D4),
    Color(0xFFC98500),
    Color(0xFFD55181),
    Color(0xFF008300),
    Color(0xFF9085E9),
    Color(0xFFE66767),
  ];
}


abstract final class IdaShadows {
  static const sm = <BoxShadow>[
    BoxShadow(color: Color(0x0F002952), blurRadius: 2, offset: Offset(0, 1)),
  ];
  static const md = <BoxShadow>[
    BoxShadow(color: Color(0x14002952), blurRadius: 12, offset: Offset(0, 4)),
  ];
  static const lg = <BoxShadow>[
    BoxShadow(color: Color(0x1F002952), blurRadius: 32, offset: Offset(0, 12)),
  ];
}


abstract final class IdaZ {
  static const sticky = 10;
  static const dropdown = 20;
  static const overlay = 30;
  static const modal = 40;
  static const toast = 50;
}


abstract final class IdaSidebar {
  static const bg = IdaColors.blue900;

  static const surface = Color(0xFF003161);
  static const text = Color(0xFFFFFFFF);

  static const textMuted = IdaColors.blue200;

  static const hover = Color(0xFF003970);
  static const active = IdaColors.blue800;
  static const border = surface;
  static const scrollbarThumb = IdaColors.blue500;
  static const scrollbarThumbHover = IdaColors.blue400;
}
