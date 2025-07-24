# Supabase Bridge Configuration

This directory contains configuration files for the Supabase Bridge Unity plugin.

## Files

- `supabase-config.json`: Contains environment profiles for Supabase projects. This file is encrypted to protect sensitive API keys.

## Security Note

The configuration file is encrypted using AES encryption with a key stored in EditorPrefs. This provides basic security for your Supabase API keys within the Unity editor environment.

## Environment Profiles

Multiple environment profiles can be configured to easily switch between different Supabase projects (e.g., development, staging, production).

Each profile contains:
- Name: A unique identifier for the profile
- SupabaseUrl: The URL of your Supabase project
- SupabaseKey: The API key for your Supabase project
- IsProduction: A flag indicating whether this is a production environment

## Do Not Commit

It is recommended to add this directory to your `.gitignore` file to avoid committing sensitive API keys to your repository.