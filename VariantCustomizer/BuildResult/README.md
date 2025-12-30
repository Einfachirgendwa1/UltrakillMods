## Variant Customizer

- Version 1.1.0
- Allows you to change colors according to weapon variation.
- Created by Einfachirgendwa1 and licensed under MIT

### How to use

After installing, you can change the weapon colors in plugins configuration.
Just navigate to Options > Plugin Config > Variant Customizer and select the weapon and variant you want to change the
colors of.

### Todos

This mod is still very early in development. Integration with the current way to change weapon colors (via the terminal)
as well as improvements to how all of this works under the hood are things I'm currently working on.

### Compilation from source (not recommended for regular users)

#### Variant 1.

Run the following command to create VariantCustomizer.zip:

```commandline
python build_zip.py
```

You can then install the created VariantCustomizer.zip by opening r2modman and navigate to Settings > Import local mod.
Then
just select VariantCustomizer.zip to install.

#### Variant 2.

Create a r2modman profile called dev and manually install all dependencies. Then run:

```commandline
python dev.py
```

The mod will not show up in r2modman if installed via Variant 2.

###### Guid: com.einfachirgendwa1.variantCustomizer