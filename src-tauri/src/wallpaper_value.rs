use crate::error::{AppError, AppResult};
use std::{
    fs,
    path::{Path, PathBuf},
};
use windows::Win32::UI::Shell::{
    DESKTOP_WALLPAPER_POSITION, DWPOS_CENTER, DWPOS_FILL, DWPOS_FIT, DWPOS_SPAN,
    DWPOS_STRETCH, DWPOS_TILE,
};

pub const NONE_MARKER: &str = "__NONE__";
pub const SOLID_PREFIX: &str = "__SOLID__:";

pub fn is_supported_fit_mode(value: &str) -> bool {
    matches!(value, "Center" | "Tile" | "Stretch" | "Fit" | "Fill" | "Span")
}

pub fn validate_fit_mode(fit_mode: &str) -> AppResult<()> {
    if !is_supported_fit_mode(fit_mode) {
        return Err(AppError::validation(format!(
            "Unsupported fit mode: {fit_mode}"
        )));
    }
    Ok(())
}

pub fn fit_str_to_position(fit: &str) -> AppResult<DESKTOP_WALLPAPER_POSITION> {
    match fit.to_ascii_lowercase().as_str() {
        "center" => Ok(DWPOS_CENTER),
        "tile" => Ok(DWPOS_TILE),
        "stretch" => Ok(DWPOS_STRETCH),
        "fit" => Ok(DWPOS_FIT),
        "fill" => Ok(DWPOS_FILL),
        "span" => Ok(DWPOS_SPAN),
        _ => Err(AppError::validation(format!("Unsupported fit mode: {fit}"))),
    }
}

pub fn position_to_fit_str(pos: DESKTOP_WALLPAPER_POSITION) -> &'static str {
    match pos {
        DWPOS_CENTER => "Center",
        DWPOS_TILE => "Tile",
        DWPOS_STRETCH => "Stretch",
        DWPOS_FIT => "Fit",
        DWPOS_FILL => "Fill",
        DWPOS_SPAN => "Span",
        _ => "Fill",
    }
}

fn parse_hex_color(color: &str) -> Option<(u8, u8, u8)> {
    let c = color.trim().trim_start_matches('#');
    if c.len() != 6 {
        return None;
    }
    let r = u8::from_str_radix(&c[0..2], 16).ok()?;
    let g = u8::from_str_radix(&c[2..4], 16).ok()?;
    let b = u8::from_str_radix(&c[4..6], 16).ok()?;
    Some((r, g, b))
}

fn solid_cache_dir() -> AppResult<PathBuf> {
    let base = dirs::data_dir()
        .or_else(dirs::config_dir)
        .ok_or_else(|| AppError::runtime("Cannot determine app data directory"))?;
    let directory = base.join("WallpaperManager").join("cache");
    fs::create_dir_all(&directory)
        .map_err(|source| AppError::io("Failed to create cache dir", source))?;
    Ok(directory)
}

fn write_solid_bmp(path: &Path, r: u8, g: u8, b: u8) -> AppResult<()> {
    let width: u32 = 64;
    let height: u32 = 64;
    let row_stride = (24 * width).div_ceil(32) * 4;
    let pixel_array_size = row_stride * height;
    let file_size: u32 = 14 + 40 + pixel_array_size;
    let mut data = Vec::with_capacity(file_size as usize);

    data.extend_from_slice(b"BM");
    data.extend_from_slice(&file_size.to_le_bytes());
    data.extend_from_slice(&0u16.to_le_bytes());
    data.extend_from_slice(&0u16.to_le_bytes());
    data.extend_from_slice(&(14u32 + 40u32).to_le_bytes());

    data.extend_from_slice(&40u32.to_le_bytes());
    data.extend_from_slice(&(width as i32).to_le_bytes());
    data.extend_from_slice(&(height as i32).to_le_bytes());
    data.extend_from_slice(&1u16.to_le_bytes());
    data.extend_from_slice(&24u16.to_le_bytes());
    data.extend_from_slice(&0u32.to_le_bytes());
    data.extend_from_slice(&pixel_array_size.to_le_bytes());
    data.extend_from_slice(&0i32.to_le_bytes());
    data.extend_from_slice(&0i32.to_le_bytes());
    data.extend_from_slice(&0u32.to_le_bytes());
    data.extend_from_slice(&0u32.to_le_bytes());

    let padding = (row_stride - (width * 3)) as usize;
    for _ in 0..height {
        for _ in 0..width {
            data.push(b);
            data.push(g);
            data.push(r);
        }
        data.extend(std::iter::repeat_n(0, padding));
    }

    fs::write(path, data).map_err(|source| AppError::io("Failed to write solid bmp", source))
}

pub fn resolve_image_path_marker(image_path: &str) -> AppResult<Option<PathBuf>> {
    let trimmed = image_path.trim();
    if trimmed.is_empty() || trimmed == NONE_MARKER {
        let path = solid_cache_dir()?.join("solid_none_black.bmp");
        if !path.exists() {
            write_solid_bmp(&path, 0, 0, 0)?;
        }
        return Ok(Some(path));
    }

    if let Some(hex) = trimmed.strip_prefix(SOLID_PREFIX) {
        let (r, g, b) = parse_hex_color(hex)
            .ok_or_else(|| AppError::validation(format!("Invalid solid color marker: {trimmed}")))?;
        let path = solid_cache_dir()?.join(format!("solid_{}_{}_{}.bmp", r, g, b));
        if !path.exists() {
            write_solid_bmp(&path, r, g, b)?;
        }
        return Ok(Some(path));
    }

    Ok(Some(PathBuf::from(trimmed)))
}

#[cfg(test)]
mod tests {
    use super::*;
    use std::{
        env, fs,
        time::{SystemTime, UNIX_EPOCH},
    };

    #[test]
    fn parses_hex_color() {
        assert_eq!(parse_hex_color("#112233"), Some((0x11, 0x22, 0x33)));
        assert_eq!(parse_hex_color("AABBCC"), Some((0xAA, 0xBB, 0xCC)));
        assert_eq!(parse_hex_color("#GG2233"), None);
        assert_eq!(parse_hex_color("#123"), None);
    }

    #[test]
    fn resolves_none_and_passthrough_markers() {
        assert!(resolve_image_path_marker("").unwrap().is_some());
        assert!(resolve_image_path_marker(NONE_MARKER).unwrap().is_some());

        let regular = r"C:\\wallpapers\\sample.png";
        assert_eq!(
            resolve_image_path_marker(regular).unwrap(),
            Some(PathBuf::from(regular))
        );
    }

    #[test]
    fn fit_mapping_is_stable() {
        assert_eq!(position_to_fit_str(fit_str_to_position("Center").unwrap()), "Center");
        assert_eq!(position_to_fit_str(fit_str_to_position("Tile").unwrap()), "Tile");
        assert_eq!(position_to_fit_str(fit_str_to_position("Stretch").unwrap()), "Stretch");
        assert_eq!(position_to_fit_str(fit_str_to_position("Fit").unwrap()), "Fit");
        assert_eq!(position_to_fit_str(fit_str_to_position("Fill").unwrap()), "Fill");
        assert_eq!(position_to_fit_str(fit_str_to_position("Span").unwrap()), "Span");
    }

    #[test]
    fn writes_valid_solid_bmp_file() {
        let mut path = env::temp_dir();
        let ts = SystemTime::now()
            .duration_since(UNIX_EPOCH)
            .unwrap()
            .as_nanos();
        path.push(format!("wallpaper_manager_test_{}.bmp", ts));

        write_solid_bmp(&path, 0x12, 0x34, 0x56).unwrap();

        let bytes = fs::read(&path).unwrap();
        assert!(bytes.len() > 54);
        assert_eq!(&bytes[0..2], b"BM");

        let _ = fs::remove_file(&path);
    }
}
